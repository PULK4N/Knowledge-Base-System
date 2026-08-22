import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  exhaustMap,
  filter,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { SkillSearchResult } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { ShortIdPipe } from '../../skills/ui/short-id.pipe';
import { Feature } from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';

type FeatureAction =
  | { readonly kind: 'status'; readonly featureId: string; readonly status: string }
  | { readonly kind: 'add-skill'; readonly featureId: string; readonly skillId: string }
  | { readonly kind: 'remove-skill'; readonly featureId: string; readonly skillId: string }
  | {
      readonly kind: 'add-record';
      readonly featureId: string;
      readonly userMessage: string;
      readonly aiAnswer: string;
    }
  | {
      readonly kind: 'update-record';
      readonly featureId: string;
      readonly recordId: string;
      readonly userMessage: string;
      readonly aiAnswer: string;
    }
  | { readonly kind: 'remove-record'; readonly featureId: string; readonly recordId: string }
  | { readonly kind: 'remove-feature'; readonly featureId: string };

type MutationState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly kind: FeatureAction['kind'] }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-feature-details-page',
  imports: [
    AsyncPipe,
    DatePipe,
    FormsModule,
    MarkdownContentComponent,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './feature-details.page.html',
  styleUrls: ['./feature-details.page.css', '../ui/feature-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly features = inject(FeatureService);
  private readonly skills = inject(SkillService);
  private readonly actions = new Subject<FeatureAction>();

  protected readonly editingRecordId = signal<string | null>(null);
  protected readonly confirmingFeatureRemoval = signal(false);

  protected readonly emptyBlocks = [];
  private readonly state$: Observable<LoadState<Feature>> =
    this.route.paramMap.pipe(
      map(params => params.get('featureId')),
      filter((featureId): featureId is string => featureId !== null),
      distinctUntilChanged(),
      switchMap(featureId =>
        this.features.watch(featureId).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  private readonly skillsState$: Observable<LoadState<SkillSearchResult>> =
    this.skills.search({ page: 1, pageSize: 100, search: '' }).pipe(
      map(data => ({ status: 'success', data }) as const),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({ status: 'error', message: toUserMessage(error) } as const),
      ),
    );

  private readonly mutation$: Observable<MutationState> = this.actions.pipe(
    exhaustMap(action =>
      this.execute(action).pipe(
        tap(() => {
          if (action.kind === 'update-record') {
            this.editingRecordId.set(null);
          }
          if (action.kind === 'remove-feature') {
            void this.router.navigate(['/features']);
          }
        }),
        map(() => ({ status: 'idle' }) as const),
        startWith({ status: 'saving', kind: action.kind } as const),
        catchError(error =>
          of({ status: 'error', message: toUserMessage(error) } as const),
        ),
      ),
    ),
    startWith({ status: 'idle' } as const),
  );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    skills: this.skillsState$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected updateStatus(featureId: string, status: string): void {
    this.actions.next({ kind: 'status', featureId, status: status.trim() });
  }

  protected addSkill(featureId: string, skillId: string): void {
    this.actions.next({ kind: 'add-skill', featureId, skillId });
  }

  protected removeSkill(featureId: string, skillId: string): void {
    this.actions.next({ kind: 'remove-skill', featureId, skillId });
  }

  protected addRecord(
    featureId: string,
    userMessage: string,
    aiAnswer: string,
  ): void {
    this.actions.next({
      kind: 'add-record',
      featureId,
      userMessage: userMessage.trim(),
      aiAnswer: aiAnswer.trim(),
    });
  }

  protected updateRecord(
    featureId: string,
    recordId: string,
    userMessage: string,
    aiAnswer: string,
  ): void {
    this.actions.next({
      kind: 'update-record',
      featureId,
      recordId,
      userMessage: userMessage.trim(),
      aiAnswer: aiAnswer.trim(),
    });
  }

  protected removeRecord(featureId: string, recordId: string): void {
    this.actions.next({ kind: 'remove-record', featureId, recordId });
  }

  protected removeFeature(featureId: string): void {
    this.actions.next({ kind: 'remove-feature', featureId });
  }

  private execute(action: FeatureAction): Observable<unknown> {
    switch (action.kind) {
      case 'status':
        return this.features.updateStatus(action.featureId, action.status);
      case 'add-skill':
        return this.features.addSkill(action.featureId, action.skillId);
      case 'remove-skill':
        return this.features.removeSkill(action.featureId, action.skillId);
      case 'add-record':
        return this.features.addRecord(action.featureId, {
          userMessage: action.userMessage,
          aiAnswer: action.aiAnswer,
        });
      case 'update-record':
        return this.features.updateRecord(action.featureId, {
          recordId: action.recordId,
          userMessage: action.userMessage,
          aiAnswer: action.aiAnswer,
        });
      case 'remove-record':
        return this.features.removeRecord(action.featureId, action.recordId);
      case 'remove-feature':
        return this.features.remove(action.featureId);
    }
  }
}
