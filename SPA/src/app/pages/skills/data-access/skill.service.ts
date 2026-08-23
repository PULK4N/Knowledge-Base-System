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
  SkillListItem,
  SkillListItemDto,
  SkillSearchRequest,
  SkillSearchResult,
  SkillSummary,
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
    const normalizedTag = request.tag.trim();
    const queryKey = JSON.stringify({
      entityType: SKILL_ENTITY_TYPE,
      page: request.page,
      pageSize: request.pageSize,
      search: normalizedSearch.toLowerCase(),
      tag: normalizedTag.toLowerCase(),
      hasReferences: request.hasReferences,
      hasAttachments: request.hasAttachments,
      sortBy: request.sortBy,
      sortDirection: request.sortDirection,
    });
    let params = new HttpParams()
      .set('page', request.page)
      .set('pageSize', request.pageSize)
      .set('sortBy', request.sortBy)
      .set('sortDirection', request.sortDirection);

    if (normalizedSearch) {
      params = params.set('search', normalizedSearch);
    }

    if (normalizedTag) {
      params = params.set('tag', normalizedTag);
    }

    if (request.hasReferences !== null) {
      params = params.set('hasReferences', request.hasReferences);
    }

    if (request.hasAttachments !== null) {
      params = params.set('hasAttachments', request.hasAttachments);
    }

    const refresh$ = this.http
      .get<PagedResult<SkillListItemDto>>(this.controllerPath, { params })
      .pipe(
        map(result => ({
          ...result,
          items: result.items.map(item => ({
            id: item.skillId,
            name: item.name,
            description: item.description,
            tags: item.tags,
            referenceCount: item.referenceCount,
            attachmentCount: item.attachmentCount,
          })),
        })),
        tap(result =>
          this.store.replaceSearch(queryKey, SKILL_ENTITY_TYPE, result),
        ),
        ignoreElements(),
      );

    const cached$ = this.store.search$<SkillListItem>(queryKey).pipe(
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
        map(skill => ({
          ...skill,
          referenceCount: Object.keys(skill.references).length,
          attachmentCount: Object.keys(skill.attachments).length,
        })),
        tap(skill => this.store.upsert(SKILL_ENTITY_TYPE, skill)),
      );
  }
}
