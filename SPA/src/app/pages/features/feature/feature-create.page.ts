import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  tap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PolicyProjectSummary } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { AddFeatureRequest } from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-feature-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './feature-create.page.html',
  styleUrls: [
    './feature-create.page.css',
    '../../skills/ui/editor-form.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureCreatePage {
  private readonly router = inject(Router);
  private readonly features = inject(FeatureService);
  private readonly policies = inject(PolicyService);
  private readonly createRequests = new Subject<AddFeatureRequest>();

  private readonly projects$: Observable<
    LoadState<readonly PolicyProjectSummary[]>
  > = this.policies
    .searchProjects({ page: 1, pageSize: 100, search: '' })
    .pipe(
      debounceTime(100),
      distinctUntilChanged(
        (previous, current) =>
          JSON.stringify(previous.items) === JSON.stringify(current.items),
      ),
      map(result => ({ status: 'success', data: result.items }) as const),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({
          status: 'error',
          message: toUserMessage(error),
        } as const),
      ),
    );

  private readonly createState$: Observable<CreateState> =
    this.createRequests.pipe(
      exhaustMap(request =>
        this.features.create(request).pipe(
          tap(feature =>
            void this.router.navigate(['/features', feature.id]),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    projects: this.projects$,
    createState: this.createState$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected create(
    projectId: string,
    name: string,
    summary: string,
    status: string,
  ): void {
    this.createRequests.next({
      projectId,
      name: name.trim(),
      summary: summary.trim(),
      status: status.trim(),
    });
  }
}
