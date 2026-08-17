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
import {
  Skill,
  UpdateSkillRequest,
} from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';

type SkillEditAction =
  | {
      readonly kind: 'save';
      readonly skillId: string;
      readonly request: UpdateSkillRequest;
    }
  | { readonly kind: 'delete'; readonly skillId: string };

type SkillMutationState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' | 'deleting' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-edit-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './skill-edit.page.html',
  styleUrl: '../ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillEditPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);
  private readonly actions = new Subject<SkillEditAction>();

  protected readonly confirmingDelete = signal(false);

  private readonly skillState$: Observable<LoadState<Skill>> =
    this.route.paramMap.pipe(
      map(params => params.get('skillId')),
      filter((skillId): skillId is string => skillId !== null),
      distinctUntilChanged(),
      switchMap(skillId =>
        this.skills.watch(skillId).pipe(
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
    );

  private readonly mutation$: Observable<SkillMutationState> =
    this.actions.pipe(
      exhaustMap(action => {
        if (action.kind === 'save') {
          return this.skills.update(action.skillId, action.request).pipe(
            tap(() => void this.router.navigate(['/skills', action.skillId])),
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

        return this.skills.delete(action.skillId).pipe(
          tap(() => void this.router.navigate(['/skills'])),
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
    state: this.skillState$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected save(
    skillId: string,
    name: string,
    description: string,
    content: string,
    tags: string,
  ): void {
    this.actions.next({
      kind: 'save',
      skillId,
      request: {
        name: name.trim(),
        description,
        content,
        tags: [...new Set(tags.split(',').map(tag => tag.trim()).filter(Boolean))],
      },
    });
  }

  protected delete(skillId: string): void {
    this.actions.next({ kind: 'delete', skillId });
  }
}
