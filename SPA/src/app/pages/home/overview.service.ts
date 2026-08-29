import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map } from 'rxjs';
import { PagedResult } from '../../core/store/entity-store.service';

export interface OverviewCounts {
  readonly skills: number;
  readonly policies: number;
  readonly memories: number;
}

@Injectable({ providedIn: 'root' })
export class OverviewService {
  private readonly http = inject(HttpClient);
  private readonly countParams = new HttpParams()
    .set('page', 1)
    .set('pageSize', 1);

  getCounts(): Observable<OverviewCounts> {
    return forkJoin({
      skills: this.count('/api/skills'),
      generalPolicies: this.count('/api/policies/general'),
      projectPolicies: this.count('/api/policies/projects'),
      topics: this.count('/api/policies/topics'),
      agentFamilies: this.count('/api/policies/agent-families'),
      memories: this.count('/api/memories'),
    }).pipe(
      map(result => ({
        skills: result.skills,
        policies:
          result.generalPolicies +
          result.projectPolicies +
          result.topics +
          result.agentFamilies,
        memories: result.memories,
      })),
    );
  }

  private count(path: string): Observable<number> {
    return this.http
      .get<PagedResult<unknown>>(path, { params: this.countParams })
      .pipe(map(result => result.totalCount));
  }
}
