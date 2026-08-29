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
    const normalizedSearch = request.search.trim();
    const normalizedAggregateId = request.aggregateId.trim();
    const queryKey = JSON.stringify({
      entityType: OUTBOX_PAYLOAD_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      search: normalizedSearch.toLowerCase(),
      onlyIncomplete: request.onlyIncomplete,
      state: request.state,
      aggregateId: normalizedAggregateId.toLowerCase(),
      sortBy: request.sortBy,
      sortDirection: request.sortDirection,
    });
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('onlyIncomplete', request.onlyIncomplete)
      .set('sortBy', request.sortBy)
      .set('sortDirection', request.sortDirection);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    if (request.state) {
      params = params.set('state', request.state);
    }

    if (normalizedAggregateId) {
      params = params.set('aggregateId', normalizedAggregateId);
    }

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
