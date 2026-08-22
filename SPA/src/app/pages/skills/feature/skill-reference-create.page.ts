import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
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
import { AddSkillReferenceRequest, Skill } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';

interface CreateReferenceAction {
  readonly skillId: string;
  readonly request: AddSkillReferenceRequest;
}

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-reference-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './skill-reference-create.page.html',
  styleUrl: '../ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillReferenceCreatePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);
  private readonly createRequests = new Subject<CreateReferenceAction>();

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
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
    );

  private readonly mutation$: Observable<CreateState> =
    this.createRequests.pipe(
      exhaustMap(({ skillId, request }) =>
        this.skills.addReference(skillId, request).pipe(
          tap(() =>
            void this.router.navigate(['/skills', skillId, 'references'], {
              queryParams: { path: request.relativePath },
            }),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.skillState$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected create(
    skillId: string,
    relativePath: string,
    content: string,
    loadAutomatically: boolean,
  ): void {
    this.createRequests.next({
      skillId,
      request: {
        relativePath: relativePath.trim(),
        content,
        loadAutomatically,
      },
    });
  }
}
