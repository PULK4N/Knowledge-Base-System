import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ProjectionGroup,
  ProjectionReplayQueuedResult,
} from './projection-administration.models';

@Injectable({ providedIn: 'root' })
export class ProjectionAdministrationService {
  private readonly http = inject(HttpClient);
  private readonly controllerPath = '/api/administration/projections';

  list(): Observable<readonly ProjectionGroup[]> {
    return this.http.get<readonly ProjectionGroup[]>(this.controllerPath);
  }

  execute(
    stateMachineId: string,
  ): Observable<ProjectionReplayQueuedResult> {
    return this.http.post<ProjectionReplayQueuedResult>(
      `${this.controllerPath}/${encodeURIComponent(stateMachineId)}/execute`,
      null,
    );
  }
}
