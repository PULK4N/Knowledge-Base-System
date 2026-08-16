import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  combineLatest,
  distinctUntilChanged,
  filter,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { SkillService } from '../data-access/skill.service';
import { MarkdownContentComponent } from '../ui/markdown-content.component';
import {
  SkillReferenceView,
  createSkillReferenceView,
} from './skill-reference.view';

interface ReferenceRouteSelection {
  readonly skillId: string;
  readonly relativePath: string | null;
}

@Component({
  selector: 'app-skill-reference-page',
  imports: [AsyncPipe, MarkdownContentComponent, RouterLink],
  templateUrl: './skill-reference.page.html',
  styleUrl: './skill-reference.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillReferencePage {
  private readonly route = inject(ActivatedRoute);
  private readonly skills = inject(SkillService);

  private readonly selection$ = combineLatest([
    this.route.paramMap,
    this.route.queryParamMap,
  ]).pipe(
    map(([params, queryParams]) => ({
      skillId: params.get('skillId'),
      relativePath: queryParams.get('path'),
    })),
    filter(
      (selection): selection is ReferenceRouteSelection =>
        selection.skillId !== null,
    ),
    distinctUntilChanged(
      (previous, current) =>
        previous.skillId === current.skillId &&
        previous.relativePath === current.relativePath,
    ),
  );

  protected readonly state$: Observable<LoadState<SkillReferenceView>> =
    this.selection$.pipe(
      switchMap(selection => {
        const relativePath = selection.relativePath;
        if (!relativePath) {
          return of({
            status: 'error',
            message: 'No skill reference was selected.',
          } as const);
        }

        return this.skills.watch(selection.skillId).pipe(
          map(skill => {
            const reference = createSkillReferenceView(
              skill,
              relativePath,
            );

            return reference
              ? ({ status: 'success', data: reference } as const)
              : ({
                  status: 'error',
                  message: 'The requested skill reference could not be found.',
                } as const);
          }),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        );
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
}
