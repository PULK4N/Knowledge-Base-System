import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
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
import { SkillService } from '../data-access/skill.service';
import {
  SkillReferenceView,
  createSkillReferenceView,
} from './skill-reference.view';

interface ReferenceSelection {
  readonly skillId: string;
  readonly relativePath: string;
}

type ReferenceEditAction =
  | {
      readonly kind: 'save';
      readonly selection: ReferenceSelection;
      readonly content: string;
      readonly loadAutomatically: boolean;
    }
  | { readonly kind: 'delete'; readonly selection: ReferenceSelection };

type ReferenceMutationState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' | 'deleting' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-reference-edit-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './skill-reference-edit.page.html',
  styleUrl: '../ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillReferenceEditPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);
  private readonly actions = new Subject<ReferenceEditAction>();

  protected readonly confirmingDelete = signal(false);

  private readonly selection$ = combineLatest([
    this.route.paramMap,
    this.route.queryParamMap,
  ]).pipe(
    map(([params, queryParams]) => ({
      skillId: params.get('skillId'),
      relativePath: queryParams.get('path'),
    })),
    filter(
      (selection): selection is ReferenceSelection =>
        selection.skillId !== null && selection.relativePath !== null,
    ),
    distinctUntilChanged(
      (previous, current) =>
        previous.skillId === current.skillId &&
        previous.relativePath === current.relativePath,
    ),
  );

  private readonly referenceState$: Observable<
    LoadState<SkillReferenceView>
  > = this.selection$.pipe(
    switchMap(selection =>
      this.skills.watch(selection.skillId).pipe(
        map(skill => createSkillReferenceView(skill, selection.relativePath)),
        map(reference =>
          reference
            ? ({ status: 'success', data: reference } as const)
            : ({
                status: 'error',
                message: 'The requested skill reference could not be found.',
              } as const),
        ),
        startWith({ status: 'loading' } as const),
        catchError(error =>
          of({
            status: 'error',
            message: toUserMessage(error),
          } as const),
        ),
      ),
    ),
  );

  private readonly mutation$: Observable<ReferenceMutationState> =
    this.actions.pipe(
      exhaustMap(action => {
        const { skillId, relativePath } = action.selection;
        if (action.kind === 'save') {
          return this.skills
            .updateReference(skillId, {
              relativePath,
              content: action.content,
              loadAutomatically: action.loadAutomatically,
            })
            .pipe(
              tap(() =>
                void this.router.navigate(
                  ['/skills', skillId, 'references'],
                  { queryParams: { path: relativePath } },
                ),
              ),
              map(() => ({ status: 'idle' }) as const),
              startWith({ status: 'saving' } as const),
              catchError(error =>
                of({
                  status: 'error',
                  message: toUserMessage(error),
                } as const),
              ),
            );
        }

        return this.skills.deleteReference(skillId, relativePath).pipe(
          tap(() =>
            void this.router.navigate(['/skills', skillId], {
              queryParams: { tab: 'references' },
            }),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'deleting' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        );
      }),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.referenceState$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected save(
    selection: ReferenceSelection,
    content: string,
    loadAutomatically: boolean,
  ): void {
    this.actions.next({
      kind: 'save',
      selection,
      content,
      loadAutomatically,
    });
  }

  protected delete(selection: ReferenceSelection): void {
    this.actions.next({ kind: 'delete', selection });
  }
}
