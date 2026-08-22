import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  filter,
  ignoreElements,
  map,
  merge,
  switchMap,
  tap,
} from 'rxjs';
import {
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  AddSkillReferenceRequest,
  AddSkillRequest,
  Skill,
  SkillAttachment,
  SkillCommandResult,
  SkillCreatedCommandResult,
  SkillDto,
  SkillSearchRequest,
  SkillSearchResult,
  SkillSummary,
  SkillSummaryDto,
  UpdateSkillReferenceRequest,
  UpdateSkillRequest,
} from './skill.models';

const SKILL_ENTITY_TYPE = 'skill';

function isSkill(entity: SkillSummary | undefined): entity is Skill {
  return !!entity && 'description' in entity && 'content' in entity;
}

@Injectable({ providedIn: 'root' })
export class SkillService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly controllerPath = '/api/skills';

  search(request: SkillSearchRequest): Observable<SkillSearchResult> {
    const normalizedSearch = request.search.trim();
    const queryKey = [
      SKILL_ENTITY_TYPE,
      request.page,
      request.pageSize,
      normalizedSearch.toLowerCase(),
    ].join(':');
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    const refresh$ = this.http
      .get<PagedResult<SkillSummaryDto>>(this.controllerPath, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(item => ({
            id: item.skillId,
            name: item.name,
          })),
        })),
        tap(result =>
          this.store.replaceSearch(queryKey, SKILL_ENTITY_TYPE, result),
        ),
        ignoreElements(),
      );

    const cached$ = this.store.search$<SkillSummary>(queryKey).pipe(
      filter(
        (result): result is SkillSearchResult => result !== undefined,
      ),
    );

    return merge(cached$, refresh$);
  }

  watch(id: string): Observable<Skill> {
    const refresh$ = this.refresh(id).pipe(ignoreElements());
    const cached$ = this.store
      .entity$<SkillSummary>(SKILL_ENTITY_TYPE, id)
      .pipe(filter(isSkill));

    return merge(cached$, refresh$);
  }

  create(request: AddSkillRequest): Observable<Skill> {
    return this.http
      .post<SkillCreatedCommandResult>(this.controllerPath, request)
      .pipe(switchMap(result => this.refresh(result.skillId)));
  }

  update(id: string, request: UpdateSkillRequest): Observable<Skill> {
    return this.http
      .post<SkillCommandResult>(
        `${this.controllerPath}/${encodeURIComponent(id)}/update`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  delete(id: string): Observable<SkillCommandResult> {
    return this.http
      .post<SkillCommandResult>(
        `${this.controllerPath}/${encodeURIComponent(id)}/delete`,
        null,
      )
      .pipe(tap(() => this.store.remove(SKILL_ENTITY_TYPE, id)));
  }

  updateReference(
    id: string,
    request: UpdateSkillReferenceRequest,
  ): Observable<Skill> {
    return this.http
      .post<SkillCommandResult>(
        `${this.controllerPath}/${encodeURIComponent(id)}/references/update`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  addReference(
    id: string,
    request: AddSkillReferenceRequest,
  ): Observable<Skill> {
    return this.http
      .post<SkillCommandResult>(
        `${this.controllerPath}/${encodeURIComponent(id)}/references`,
        request,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  addAttachments(id: string, files: readonly File[]): Observable<Skill> {
    const body = new FormData();
    files.forEach(file => body.append('files', file, file.name));

    return this.http
      .post<readonly SkillAttachment[]>(
        `${this.controllerPath}/${encodeURIComponent(id)}/attachments`,
        body,
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  deleteReference(
    id: string,
    relativePath: string,
  ): Observable<Skill> {
    return this.http
      .post<SkillCommandResult>(
        `${this.controllerPath}/${encodeURIComponent(id)}/references/delete`,
        { relativePath },
      )
      .pipe(switchMap(() => this.refresh(id)));
  }

  private refresh(id: string): Observable<Skill> {
    return this.http
      .get<SkillDto>(`${this.controllerPath}/${encodeURIComponent(id)}`)
      .pipe(
        map(skill => ({ ...skill })),
        tap(skill => this.store.upsert(SKILL_ENTITY_TYPE, skill)),
      );
  }
}
