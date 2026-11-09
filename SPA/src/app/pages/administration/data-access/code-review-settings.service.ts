import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, filter, ignoreElements, merge, tap } from 'rxjs';
import { EntityStore } from '../../../core/store/entity-store.service';
import { CodeReviewSettings } from './code-review-settings.models';

interface SettingsEntity extends CodeReviewSettings {
  readonly id: string;
}

@Injectable({ providedIn: 'root' })
export class CodeReviewSettingsService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(EntityStore);
  private readonly path = '/api/settings/code-review';
  private readonly entityType = 'code-review-settings';
  private readonly id = 'global';

  watch(): Observable<CodeReviewSettings> {
    const cached$ = this.store.entity$<SettingsEntity>(this.entityType, this.id).pipe(
      filter((value): value is SettingsEntity => value !== undefined),
    );
    const refresh$ = this.http.get<CodeReviewSettings>(this.path).pipe(
      tap(value => this.upsert(value)),
      ignoreElements(),
    );
    return merge(cached$, refresh$);
  }

  save(value: CodeReviewSettings): Observable<CodeReviewSettings> {
    return this.http.post<CodeReviewSettings>(this.path, value).pipe(
      tap(result => this.upsert(result)),
    );
  }

  private upsert(value: CodeReviewSettings): void {
    this.store.upsert<SettingsEntity>(this.entityType, { ...value, id: this.id });
  }
}
