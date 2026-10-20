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
import { ClampOverflowDirective } from '../../../shared/clamp-overflow/clamp-overflow.directive';
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
  AddFeaturePlanRequest,
  AddFeatureRecordRequest,
  FEATURE_RECORD_TYPE_SUGGESTIONS,
  Feature,
  FeaturePlan,
  FeatureRecord,
  FeatureRecordSourceType,
  SetFeatureBranchRequest,
  UpdateFeatureRecordRequest,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import { FeatureBranchOverviewComponent } from '../ui/feature-branch-overview.component';
import { toRecordOptions } from '../ui/feature-link-options';
import {
  FeatureLinkOption,
  FeatureLinkPickerComponent,
} from '../ui/feature-link-picker.component';
import {
  FEATURE_PLAN_STATUS_OPTIONS,
  FeaturePlanStatusPipe,
} from '../ui/feature-plan-status';
import {
  featureRecordTypes,
  parseFeatureDetailListState,
  selectFeaturePlans,
  selectFeatureRecords,
  selectFeatureReviewNotes,
} from './feature-detail-list-state';
import { toFeatureHistory } from './feature-history';

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
const RECORD_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created date' },
  { value: 'title', label: 'Title' },
  { value: 'type', label: 'Type' },
];
const REVIEW_NOTE_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: 'All', label: 'All review notes' },
  { value: 'Edited', label: 'Edited notes' },
  { value: 'Original', label: 'Original notes' },
];
const REVIEW_NOTE_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created date' },
  { value: 'title', label: 'Title' },
];

type FeatureAction =
  | { readonly kind: 'status'; readonly featureId: string; readonly status: string }
  | { readonly kind: 'summary'; readonly featureId: string; readonly summary: string }
  | { readonly kind: 'add-skill'; readonly featureId: string; readonly skillId: string }
  | { readonly kind: 'remove-skill'; readonly featureId: string; readonly skillId: string }
  | {
      readonly kind: 'add-record';
      readonly featureId: string;
      readonly request: AddFeatureRecordRequest;
    }
  | {
      readonly kind: 'update-record';
      readonly featureId: string;
      readonly request: UpdateFeatureRecordRequest;
    }
  | { readonly kind: 'remove-record'; readonly featureId: string; readonly recordId: string }
  | {
      readonly kind: 'add-review-note';
      readonly featureId: string;
      readonly title: string;
      readonly content: string;
    }
  | {
      readonly kind: 'update-review-note';
      readonly featureId: string;
      readonly reviewNoteId: string;
      readonly title: string;
      readonly content: string;
    }
  | {
      readonly kind: 'remove-review-note';
      readonly featureId: string;
      readonly reviewNoteId: string;
    }
  | {
      readonly kind: 'add-plan';
      readonly featureId: string;
      readonly request: AddFeaturePlanRequest;
    }
  | { readonly kind: 'remove-plan'; readonly featureId: string; readonly planId: string }
  | {
      readonly kind: 'set-branch';
      readonly featureId: string;
      readonly request: SetFeatureBranchRequest;
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
    ClampOverflowDirective,
    DatePipe,
    FeatureBranchOverviewComponent,
    FeatureLinkPickerComponent,
    FeaturePlanStatusPipe,
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

  protected readonly editingSummary = signal(false);
  protected readonly editingStatus = signal(false);
  protected readonly expandedStatus = signal(false);
  /** Set from the clamped status element; the toggle only shows when text is hidden. */
  protected readonly statusOverflows = signal(false);
  protected readonly editingRecordId = signal<string | null>(null);
  protected readonly editingReviewNoteId = signal<string | null>(null);
  protected readonly addingReviewNote = signal(false);
  protected readonly addingPlan = signal(false);
  /** Records picked in the add-plan form. */
  protected readonly newPlanRecordIds = signal<readonly string[]>([]);
  protected readonly addingRecord = signal(false);
  /** Plans picked in the add-record form; only plans store the link. */
  protected readonly newRecordPlanIds = signal<readonly string[]>([]);
  protected readonly confirmingPlanRemoval = signal<string | null>(null);
  protected readonly confirmingFeatureRemoval = signal(false);

  protected readonly emptyBlocks = [];
  protected readonly planFilterOptions = PLAN_FILTER_OPTIONS;
  protected readonly planSortOptions = PLAN_SORT_OPTIONS;
  protected readonly planStatusOptions = FEATURE_PLAN_STATUS_OPTIONS;
  protected readonly recordSortOptions = RECORD_SORT_OPTIONS;
  protected readonly recordTypeSuggestions = FEATURE_RECORD_TYPE_SUGGESTIONS;
  protected readonly reviewNoteFilterOptions = REVIEW_NOTE_FILTER_OPTIONS;
  protected readonly reviewNoteSortOptions = REVIEW_NOTE_SORT_OPTIONS;

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
          if (action.kind === 'summary') {
            this.editingSummary.set(false);
          }
          if (action.kind === 'status') {
            this.editingStatus.set(false);
          }
          if (action.kind === 'update-record') {
            this.editingRecordId.set(null);
          }
          if (action.kind === 'update-review-note') {
            this.editingReviewNoteId.set(null);
          }
          if (action.kind === 'add-review-note') {
            this.addingReviewNote.set(false);
          }
          if (action.kind === 'add-record') {
            this.addingRecord.set(false);
            this.newRecordPlanIds.set([]);
          }
          if (action.kind === 'add-plan') {
            this.addingPlan.set(false);
            this.newPlanRecordIds.set([]);
          }
          if (action.kind === 'remove-plan') {
            this.confirmingPlanRemoval.set(null);
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
      skillNames: new Map(
        vm.skills.status === 'success'
          ? vm.skills.data.items.map(skill => [skill.id, skill.name] as const)
          : [],
      ),
      collections:
        vm.state.status === 'success'
          ? {
              plans: selectFeaturePlans(
                vm.state.data.plans,
                vm.listState.plans,
              ),
              records: selectFeatureRecords(
                vm.state.data.records,
                vm.listState.records,
              ),
              recordTypeOptions: toRecordTypeOptions(
                vm.state.data.records,
                vm.listState.records.filter,
              ),
              recordsById: new Map(
                vm.state.data.records.map(record => [record.id, record] as const),
              ),
              plansByRecordId: toPlansByRecordId(vm.state.data.plans),
              recordOptions: toRecordOptions(vm.state.data.records),
              planOptions: vm.state.data.plans.map(
                (plan): FeatureLinkOption => ({
                  id: plan.id,
                  label: plan.title,
                  detail: plan.branch?.name ?? '',
                }),
              ),
              reviewNotes: selectFeatureReviewNotes(
                vm.state.data.reviewNotes,
                vm.listState.reviewNotes,
              ),
              history: toFeatureHistory(vm.state.data.memoryHistory),
            }
          : {
              plans: [],
              records: [],
              recordTypeOptions: [],
              recordsById: new Map<string, FeatureRecord>(),
              plansByRecordId: new Map<string, readonly FeaturePlan[]>(),
              recordOptions: [],
              planOptions: [],
              reviewNotes: [],
              history: [],
            },
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

  protected updateRecordSearch(search: string): void {
    this.updateListQuery({ recordSearch: omitEmpty(search) }, true);
  }

  protected updateRecordFilter(type: string): void {
    this.updateListQuery({ recordType: omitDefault(type, 'All') });
  }

  protected updateRecordSort(sortBy: string): void {
    if (!RECORD_SORT_OPTIONS.some(option => option.value === sortBy)) return;
    this.updateListQuery({ recordSort: omitDefault(sortBy, 'updatedAt') });
  }

  protected updateRecordDirection(direction: ListSortDirection): void {
    this.updateListQuery({
      recordDirection: omitDefault(direction, 'Descending'),
    });
  }

  protected updateReviewNoteSearch(search: string): void {
    this.updateListQuery({ reviewNoteSearch: omitEmpty(search) }, true);
  }

  protected updateReviewNoteFilter(filter: string): void {
    if (!REVIEW_NOTE_FILTER_OPTIONS.some(option => option.value === filter)) {
      return;
    }
    this.updateListQuery({ reviewNoteFilter: omitDefault(filter, 'All') });
  }

  protected updateReviewNoteSort(sortBy: string): void {
    if (!REVIEW_NOTE_SORT_OPTIONS.some(option => option.value === sortBy)) {
      return;
    }
    this.updateListQuery({ reviewNoteSort: omitDefault(sortBy, 'updatedAt') });
  }

  protected updateReviewNoteDirection(direction: ListSortDirection): void {
    this.updateListQuery({
      reviewNoteDirection: omitDefault(direction, 'Descending'),
    });
  }

  protected fieldText(value: string, fallback: string): string {
    const text = value.trim();

    return text.length > 0 ? text : fallback;
  }

  protected updateStatus(featureId: string, status: string): void {
    this.actions.next({ kind: 'status', featureId, status: status.trim() });
  }

  protected updateSummary(featureId: string, summary: string): void {
    this.actions.next({ kind: 'summary', featureId, summary: summary.trim() });
  }

  protected addSkill(featureId: string, skillId: string): void {
    this.actions.next({ kind: 'add-skill', featureId, skillId });
  }

  protected removeSkill(featureId: string, skillId: string): void {
    this.actions.next({ kind: 'remove-skill', featureId, skillId });
  }

  protected addRecord(
    featureId: string,
    type: string,
    title: string,
    content: string,
    sourceType: string,
    sourceReference: string,
  ): void {
    this.actions.next({
      kind: 'add-record',
      featureId,
      request: {
        ...toRecordContent(type, title, content, sourceType, sourceReference),
        planIds: this.newRecordPlanIds(),
      },
    });
  }

  protected updateRecord(
    featureId: string,
    recordId: string,
    type: string,
    title: string,
    content: string,
    sourceType: string,
    sourceReference: string,
  ): void {
    this.actions.next({
      kind: 'update-record',
      featureId,
      request: {
        ...toRecordContent(type, title, content, sourceType, sourceReference),
        recordId,
      },
    });
  }

  protected removeRecord(featureId: string, recordId: string): void {
    this.actions.next({ kind: 'remove-record', featureId, recordId });
  }

  protected addReviewNote(
    featureId: string,
    title: string,
    content: string,
  ): void {
    this.actions.next({
      kind: 'add-review-note',
      featureId,
      title: title.trim(),
      content: content.trim(),
    });
  }

  protected updateReviewNote(
    featureId: string,
    reviewNoteId: string,
    title: string,
    content: string,
  ): void {
    this.actions.next({
      kind: 'update-review-note',
      featureId,
      reviewNoteId,
      title: title.trim(),
      content: content.trim(),
    });
  }

  protected removeReviewNote(featureId: string, reviewNoteId: string): void {
    this.actions.next({
      kind: 'remove-review-note',
      featureId,
      reviewNoteId,
    });
  }

  protected addPlan(featureId: string, request: AddFeaturePlanRequest): void {
    this.actions.next({
      kind: 'add-plan',
      featureId,
      request: {
        ...request,
        title: request.title.trim(),
        branchName: request.branchName.trim(),
        branchDescription: request.branchDescription.trim(),
        additionalInfo: request.additionalInfo.trim(),
        recordIds: this.newPlanRecordIds(),
      },
    });
  }

  protected removePlan(featureId: string, planId: string): void {
    this.actions.next({ kind: 'remove-plan', featureId, planId });
  }

  protected setBranch(featureId: string, request: SetFeatureBranchRequest): void {
    this.actions.next({ kind: 'set-branch', featureId, request });
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
      case 'summary':
        return this.features.updateSummary(action.featureId, action.summary);
      case 'add-skill':
        return this.features.addSkill(action.featureId, action.skillId);
      case 'remove-skill':
        return this.features.removeSkill(action.featureId, action.skillId);
      case 'add-record':
        return this.features.addRecord(action.featureId, action.request);
      case 'update-record':
        return this.features.updateRecord(action.featureId, action.request);
      case 'remove-record':
        return this.features.removeRecord(action.featureId, action.recordId);
      case 'add-review-note':
        return this.features.addReviewNote(action.featureId, {
          title: action.title,
          content: action.content,
        });
      case 'update-review-note':
        return this.features.updateReviewNote(action.featureId, {
          reviewNoteId: action.reviewNoteId,
          title: action.title,
          content: action.content,
        });
      case 'remove-review-note':
        return this.features.removeReviewNote(
          action.featureId,
          action.reviewNoteId,
        );
      case 'add-plan':
        return this.features.addPlan(action.featureId, action.request);
      case 'remove-plan':
        return this.features.removePlan(action.featureId, action.planId);
      case 'set-branch':
        return this.features.setBranch(action.featureId, action.request);
      case 'remove-feature':
        return this.features.remove(action.featureId);
    }
  }
}

const SOURCE_TYPES: readonly FeatureRecordSourceType[] = [
  'Other',
  'Code',
  'Web',
  'Mcp',
];

function toRecordContent(
  type: string,
  title: string,
  content: string,
  sourceType: string,
  sourceReference: string,
) {
  const source = SOURCE_TYPES.find(value => value === sourceType) ?? null;

  return {
    type: type.trim().toLowerCase(),
    title: title.trim(),
    content: content.trim(),
    sourceType: source,
    sourceReference: source === null ? '' : sourceReference.trim(),
  };
}

/** The type filter lists every type in use, plus the selected one if no record has it. */
function toRecordTypeOptions(
  records: readonly FeatureRecord[],
  selected: string,
): readonly ListControlOption[] {
  const types = featureRecordTypes(records);
  const shown =
    selected === 'All' || types.includes(selected) ? types : [...types, selected];

  return [
    { value: 'All', label: 'All types' },
    ...shown.map(type => ({ value: type, label: type })),
  ];
}

function toPlansByRecordId(
  plans: readonly FeaturePlan[],
): ReadonlyMap<string, readonly FeaturePlan[]> {
  const plansByRecordId = new Map<string, FeaturePlan[]>();

  for (const plan of plans) {
    for (const recordId of plan.recordIds) {
      plansByRecordId.set(recordId, [
        ...(plansByRecordId.get(recordId) ?? []),
        plan,
      ]);
    }
  }

  return plansByRecordId;
}
