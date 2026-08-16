import { AsyncPipe, DecimalPipe, KeyValuePipe } from '@angular/common';
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
import { Skill } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';
import {
  MarkdownBlock,
  parseMarkdownBlocks,
} from '../ui/markdown-blocks';
import { MarkdownContentComponent } from '../ui/markdown-content.component';
import { ShortIdPipe } from '../ui/short-id.pipe';
import { parseSkillTab } from './skill-tabs';

interface SkillDetailsView {
  readonly skill: Skill;
  readonly blocks: readonly MarkdownBlock[];
}

@Component({
  selector: 'app-skill-details-page',
  imports: [
    AsyncPipe,
    DecimalPipe,
    KeyValuePipe,
    MarkdownContentComponent,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './skill-details.page.html',
  styleUrl: './skill-details.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly skills = inject(SkillService);

  private readonly activeTab$ = this.route.queryParamMap.pipe(
    map(params => parseSkillTab(params.get('tab'))),
    distinctUntilChanged(),
  );

  private readonly skillState$: Observable<LoadState<SkillDetailsView>> =
    this.route.paramMap.pipe(
      map(params => params.get('skillId')),
      filter((skillId): skillId is string => skillId !== null),
      distinctUntilChanged(),
      switchMap(skillId =>
        this.skills.watch(skillId).pipe(
          map(skill => ({
            status: 'success',
            data: {
              skill,
              blocks: parseMarkdownBlocks(skill.content),
            },
          }) as const),
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

  protected readonly vm$ = combineLatest({
    state: this.skillState$,
    activeTab: this.activeTab$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));
}
