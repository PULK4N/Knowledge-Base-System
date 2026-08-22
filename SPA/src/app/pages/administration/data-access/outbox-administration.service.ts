import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  filter,
  ignoreElements,
  map,
  merge,
  tap,
} from 'rxjs';
import {
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  OutboxPayload,
  OutboxPayloadDto,
  OutboxPayloadSearchRequest,
  OutboxPayloadSearchResult,
} from './outbox-administration.models';

const OUTBOX_PAYLOAD_ENTITY_TYPE = 'outbox-payload';

function formatJson(value: string): string {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function fromDto(payload: OutboxPayloadDto): OutboxPayload {
  return {
    ...payload,
    id: String(payload.id),
    payloadId: payload.id,
    executionInfoJson: formatJson(payload.executionInfoJson),
    eventDataJson: formatJson(payload.eventDataJson),
  };
}

@Injectable({ providedIn: 'root' })
export class OutboxAdministrationService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/administration/outbox';

  search(
    request: OutboxPayloadSearchRequest,
  ): Observable<OutboxPayloadSearchResult> {
    const queryKey = [
      OUTBOX_PAYLOAD_ENTITY_TYPE,
      request.page,
      request.pageSize,
      request.onlyIncomplete,
    ].join(':');
    const params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('onlyIncomplete', request.onlyIncomplete);
    const refresh$ = this.http
      .get<PagedResult<OutboxPayloadDto>>(this.controllerPath, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(fromDto),
        })),
        tap(result =>
          this.store.replaceSearch(
            queryKey,
            OUTBOX_PAYLOAD_ENTITY_TYPE,
            result,
          ),
        ),
        ignoreElements(),
      );
    const cached$ = this.store.search$<OutboxPayload>(queryKey).pipe(
      filter(
        (result): result is OutboxPayloadSearchResult =>
          result !== undefined,
      ),
    );

    return merge(cached$, refresh$);
  }

  requeue(outboxPayloadId: string): Observable<OutboxPayload> {
    return this.http
      .post<OutboxPayloadDto>(
        `${this.controllerPath}/${encodeURIComponent(outboxPayloadId)}/requeue`,
        null,
      )
      .pipe(
        map(fromDto),
        tap(payload =>
          this.store.upsert(OUTBOX_PAYLOAD_ENTITY_TYPE, payload),
        ),
      );
  }
}
