import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, concat, map, of, switchMap, take, tap } from 'rxjs';
import {
  EntityStore,
  PagedResult,
} from '../../../core/store/entity-store.service';
import {
  Skill,
  SkillDto,
  SkillSearchRequest,
  SkillSearchResult,
  SkillSummary,
  SkillSummaryDto,
} from './skill.models';

const SKILL_ENTITY_TYPE = 'skill';

function isSkill(entity: SkillSummary): entity is Skill {
  return 'description' in entity && 'content' in entity;
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
      );

    return this.store.search$<SkillSummary>(queryKey).pipe(
      take(1),
      switchMap(cached => (cached ? concat(of(cached), refresh$) : refresh$)),
    );
  }

  watch(id: string): Observable<Skill> {
    const refresh$ = this.http
      .get<SkillDto>(`${this.controllerPath}/${encodeURIComponent(id)}`)
      .pipe(
        map(skill => ({ ...skill })),
        tap(skill => this.store.upsert(SKILL_ENTITY_TYPE, skill)),
      );

    return this.store.entity$<SkillSummary>(SKILL_ENTITY_TYPE, id).pipe(
      take(1),
      switchMap(cached =>
        cached && isSkill(cached)
          ? concat(of(cached), refresh$)
          : refresh$,
      ),
    );
  }
}
