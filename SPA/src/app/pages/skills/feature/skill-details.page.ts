import {
  AsyncPipe,
  DatePipe,
  DecimalPipe,
  KeyValuePipe,
} from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { Skill } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';
import { PolicyService } from '../../policies/data-access/policy.service';
import {
  MarkdownBlock,
  parseMarkdownBlocks,
} from '../ui/markdown-blocks';
import { MarkdownContentComponent } from '../ui/markdown-content.component';
import { SkillHistoryItem, toSkillHistory } from './skill-history';
import {
  moveScenario,
  removeScenario,
  replaceScenario,
  toSavedScenarios,
} from './skill-scenarios';
import { parseSkillTab } from './skill-tabs';

interface SkillDetailsView {
  readonly skill: Skill;
  readonly blocks: readonly MarkdownBlock[];
  readonly history: readonly SkillHistoryItem[];
}

interface ScenarioSave {
  readonly skillId: string;
  readonly scenarios: readonly string[];
}

type ScenarioMutationState =
  | { readonly status: 'idle' | 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-details-page',
  imports: [
    AsyncPipe,
    DatePipe,
    DecimalPipe,
    KeyValuePipe,
    MarkdownContentComponent,
    RouterLink,
  ],
  templateUrl: './skill-details.page.html',
  styleUrl: './skill-details.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly skills = inject(SkillService);
  private readonly policies = inject(PolicyService);
  private readonly scenarioSaves = new Subject<ScenarioSave>();

  /** The scenario list being edited, or null when not editing. */
  protected readonly scenarioDraft = signal<readonly string[] | null>(null);

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
              history: toSkillHistory(skill.memoryHistory),
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

  private readonly scenarioMutation$: Observable<ScenarioMutationState> =
    this.scenarioSaves.pipe(
      exhaustMap(save =>
        this.skills.setUseScenarios(save.skillId, save.scenarios).pipe(
          tap(() => this.scenarioDraft.set(null)),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  /** Project names for the "Used by projects" links; IDs show when a name is unknown. */
  private readonly projectNames$: Observable<ReadonlyMap<string, string>> =
    this.policies.searchProjects({ page: 1, pageSize: 100, search: '' }).pipe(
      map(result => new Map(result.items.map(project => [project.id, project.name]))),
      catchError(() => of(new Map<string, string>())),
      startWith(new Map<string, string>()),
    );

  protected readonly vm$ = combineLatest({
    state: this.skillState$,
    activeTab: this.activeTab$,
    scenarioMutation: this.scenarioMutation$,
    projectNames: this.projectNames$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected startEditingScenarios(scenarios: readonly string[]): void {
    this.scenarioDraft.set(scenarios.length > 0 ? scenarios : ['']);
  }

  protected cancelEditingScenarios(): void {
    this.scenarioDraft.set(null);
  }

  protected addScenario(): void {
    this.scenarioDraft.update(draft => [...(draft ?? []), '']);
  }

  protected updateScenario(index: number, value: string): void {
    this.scenarioDraft.update(draft =>
      draft ? replaceScenario(draft, index, value) : draft,
    );
  }

  protected moveScenario(index: number, offset: -1 | 1): void {
    this.scenarioDraft.update(draft =>
      draft ? moveScenario(draft, index, offset) : draft,
    );
  }

  protected removeScenario(index: number): void {
    this.scenarioDraft.update(draft =>
      draft ? removeScenario(draft, index) : draft,
    );
  }

  protected canSaveScenarios(draft: readonly string[]): boolean {
    return toSavedScenarios(draft) !== null;
  }

  protected saveScenarios(skillId: string, draft: readonly string[]): void {
    const scenarios = toSavedScenarios(draft);
    if (scenarios) this.scenarioSaves.next({ skillId, scenarios });
  }
}
