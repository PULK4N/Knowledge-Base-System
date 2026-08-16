import { AsyncPipe, DecimalPipe, KeyValuePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  BehaviorSubject,
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
import { ShortIdPipe } from '../ui/short-id.pipe';

type SkillTab = 'content' | 'references' | 'attachments';

interface SkillDetailsView {
  readonly skill: Skill;
  readonly blocks: readonly MarkdownBlock[];
}

@Component({
  selector: 'app-skill-details-page',
  imports: [AsyncPipe, DecimalPipe, KeyValuePipe, RouterLink, ShortIdPipe],
  templateUrl: './skill-details.page.html',
  styleUrl: './skill-details.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly skills = inject(SkillService);
  private readonly activeTabSubject = new BehaviorSubject<SkillTab>('content');

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
    activeTab: this.activeTabSubject,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected selectTab(tab: SkillTab): void {
    this.activeTabSubject.next(tab);
  }
}
