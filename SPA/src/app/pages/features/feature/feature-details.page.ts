import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
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
  ListControlOption,
  ListControlsComponent,
} from '../../../shared/list-controls/list-controls.component';
import { omitDefault, omitEmpty } from '../../../shared/list-state/list-route-state';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { SkillSearchResult } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { ShortIdPipe } from '../../skills/ui/short-id.pipe';
import {
  Feature,
  FeatureResearchDiscoverySourceType,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import {
  parseFeatureDetailListState,
  selectFeaturePlans,
  selectFeatureRecords,
  selectFeatureResearch,
} from './feature-detail-list-state';

const PLAN_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: 'All', label: 'All content types' },
  { value: 'Markdown', label: 'Markdown' },
  { value: 'Html', label: 'HTML' },
];
const PLAN_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created date' },
  { value: 'title', label: 'Title' },
];
const RESEARCH_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: 'All', label: 'All sources' },
  { value: 'Code', label: 'Code' },
  { value: 'Web', label: 'Web' },
  { value: 'Mcp', label: 'MCP' },
  { value: 'Other', label: 'Other' },
];
const RESEARCH_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created date' },
  { value: 'title', label: 'Title' },
  { value: 'sourceType', label: 'Source type' },
];
const CONVERSATION_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: 'All', label: 'All records' },
  { value: 'Edited', label: 'Edited records' },
  { value: 'Original', label: 'Original records' },
];
const CONVERSATION_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created date' },
  { value: 'userMessage', label: 'User message' },
];

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
  | {
      readonly kind: 'add-research-discovery';
      readonly featureId: string;
      readonly title: string;
      readonly content: string;
      readonly sourceType: FeatureResearchDiscoverySourceType;
      readonly sourceReference: string;
    }
  | {
      readonly kind: 'update-research-discovery';
      readonly featureId: string;
      readonly discoveryId: string;
      readonly title: string;
      readonly content: string;
      readonly sourceType: FeatureResearchDiscoverySourceType;
      readonly sourceReference: string;
    }
  | {
      readonly kind: 'remove-research-discovery';
      readonly featureId: string;
      readonly discoveryId: string;
    }
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
    ListControlsComponent,
    MarkdownContentComponent,
    RouterLink,
    ShortIdPipe,
  ],
  templateUrl: './feature-details.page.html',
  styleUrls: [
    './feature-details.page.css',
    './feature-detail-tabs.css',
    '../ui/feature-pages.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly features = inject(FeatureService);
  private readonly skills = inject(SkillService);
  private readonly actions = new Subject<FeatureAction>();

  protected readonly editingRecordId = signal<string | null>(null);
  protected readonly editingResearchDiscoveryId = signal<string | null>(null);
  protected readonly confirmingFeatureRemoval = signal(false);

  protected readonly emptyBlocks = [];
  protected readonly planFilterOptions = PLAN_FILTER_OPTIONS;
  protected readonly planSortOptions = PLAN_SORT_OPTIONS;
  protected readonly researchFilterOptions = RESEARCH_FILTER_OPTIONS;
  protected readonly researchSortOptions = RESEARCH_SORT_OPTIONS;
  protected readonly conversationFilterOptions = CONVERSATION_FILTER_OPTIONS;
  protected readonly conversationSortOptions = CONVERSATION_SORT_OPTIONS;

  private readonly listState$ = this.route.queryParamMap.pipe(
    map(parseFeatureDetailListState),
    distinctUntilChanged(
      (previous, current) =>
        JSON.stringify(previous) === JSON.stringify(current),
    ),
  );

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
    this.skills.search({
      page: 1,
      pageSize: 100,
      search: '',
      tag: '',
      hasReferences: null,
      hasAttachments: null,
      sortBy: 'Name',
      sortDirection: 'Ascending',
    }).pipe(
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
          if (action.kind === 'update-research-discovery') {
            this.editingResearchDiscoveryId.set(null);
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
    listState: this.listState$,
    skills: this.skillsState$,
    mutation: this.mutation$,
  }).pipe(
    map(vm => ({
      ...vm,
      collections:
        vm.state.status === 'success'
          ? {
              plans: selectFeaturePlans(
                vm.state.data.plans,
                vm.listState.plans,
              ),
              research: selectFeatureResearch(
                vm.state.data.researchDiscoveries,
                vm.listState.research,
              ),
              conversations: selectFeatureRecords(
                vm.state.data.records,
                vm.listState.conversations,
              ),
            }
          : { plans: [], research: [], conversations: [] },
    })),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected updatePlanSearch(search: string): void {
    this.updateListQuery({ planSearch: omitEmpty(search) }, true);
  }

  protected updatePlanFilter(filter: string): void {
    if (!PLAN_FILTER_OPTIONS.some(option => option.value === filter)) return;
    this.updateListQuery({ planType: omitDefault(filter, 'All') });
  }

  protected updatePlanSort(sortBy: string): void {
    if (!PLAN_SORT_OPTIONS.some(option => option.value === sortBy)) return;
    this.updateListQuery({ planSort: omitDefault(sortBy, 'updatedAt') });
  }

  protected updatePlanDirection(direction: ListSortDirection): void {
    this.updateListQuery({
      planDirection: omitDefault(direction, 'Descending'),
    });
  }

  protected updateResearchSearch(search: string): void {
    this.updateListQuery({ researchSearch: omitEmpty(search) }, true);
  }

  protected updateResearchFilter(filter: string): void {
    if (!RESEARCH_FILTER_OPTIONS.some(option => option.value === filter)) return;
    this.updateListQuery({ researchSource: omitDefault(filter, 'All') });
  }

  protected updateResearchSort(sortBy: string): void {
    if (!RESEARCH_SORT_OPTIONS.some(option => option.value === sortBy)) return;
    this.updateListQuery({
      researchSort: omitDefault(sortBy, 'updatedAt'),
    });
  }

  protected updateResearchDirection(direction: ListSortDirection): void {
    this.updateListQuery({
      researchDirection: omitDefault(direction, 'Descending'),
    });
  }

  protected updateConversationSearch(search: string): void {
    this.updateListQuery({ conversationSearch: omitEmpty(search) }, true);
  }

  protected updateConversationFilter(filter: string): void {
    if (!CONVERSATION_FILTER_OPTIONS.some(option => option.value === filter)) {
      return;
    }
    this.updateListQuery({
      conversationFilter: omitDefault(filter, 'All'),
    });
  }

  protected updateConversationSort(sortBy: string): void {
    if (!CONVERSATION_SORT_OPTIONS.some(option => option.value === sortBy)) {
      return;
    }
    this.updateListQuery({
      conversationSort: omitDefault(sortBy, 'updatedAt'),
    });
  }

  protected updateConversationDirection(direction: ListSortDirection): void {
    this.updateListQuery({
      conversationDirection: omitDefault(direction, 'Descending'),
    });
  }

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

  protected addResearchDiscovery(
    featureId: string,
    title: string,
    content: string,
    sourceType: FeatureResearchDiscoverySourceType,
    sourceReference: string,
  ): void {
    this.actions.next({
      kind: 'add-research-discovery',
      featureId,
      title: title.trim(),
      content: content.trim(),
      sourceType,
      sourceReference: sourceReference.trim(),
    });
  }

  protected updateResearchDiscovery(
    featureId: string,
    discoveryId: string,
    title: string,
    content: string,
    sourceType: FeatureResearchDiscoverySourceType,
    sourceReference: string,
  ): void {
    this.actions.next({
      kind: 'update-research-discovery',
      featureId,
      discoveryId,
      title: title.trim(),
      content: content.trim(),
      sourceType,
      sourceReference: sourceReference.trim(),
    });
  }

  protected removeResearchDiscovery(
    featureId: string,
    discoveryId: string,
  ): void {
    this.actions.next({
      kind: 'remove-research-discovery',
      featureId,
      discoveryId,
    });
  }

  protected removeFeature(featureId: string): void {
    this.actions.next({ kind: 'remove-feature', featureId });
  }

  private updateListQuery(queryParams: Params, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
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
      case 'add-research-discovery':
        return this.features.addResearchDiscovery(action.featureId, {
          title: action.title,
          content: action.content,
          sourceType: action.sourceType,
          sourceReference: action.sourceReference,
        });
      case 'update-research-discovery':
        return this.features.updateResearchDiscovery(action.featureId, {
          discoveryId: action.discoveryId,
          title: action.title,
          content: action.content,
          sourceType: action.sourceType,
          sourceReference: action.sourceReference,
        });
      case 'remove-research-discovery':
        return this.features.removeResearchDiscovery(
          action.featureId,
          action.discoveryId,
        );
      case 'remove-feature':
        return this.features.remove(action.featureId);
    }
  }
}
