import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { UniqueConstraintRebuildResult } from './unique-constraint-administration.models';

@Injectable({ providedIn: 'root' })
export class UniqueConstraintAdministrationService {
  private readonly http = inject(HttpClient);
  private readonly controllerPath = '/api/administration/unique-constraints';

  listStateMachines(): Observable<readonly string[]> {
    return this.http.get<readonly string[]>(
      `${this.controllerPath}/state-machines`,
    );
  }

  rebuild(stateMachineId: string): Observable<UniqueConstraintRebuildResult> {
    return this.http.post<UniqueConstraintRebuildResult>(
      `${this.controllerPath}/${encodeURIComponent(stateMachineId)}/rebuild`,
      null,
    );
  }
}
