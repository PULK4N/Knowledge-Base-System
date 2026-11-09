import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { Subject } from 'rxjs';
import { ListControlsComponent } from '../../../shared/list-controls/list-controls.component';
import {
  ListFilter,
  ListFilterChange,
  ListFiltersComponent,
} from '../../../shared/list-filters/list-filters.component';
import { ListRowComponent } from '../../../shared/list-row/list-row.component';
import {
  ListSortDirection,
  selectClientList,
} from '../../../shared/list-state/list-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { pageOf } from './page-of';
import {
  DEFAULT_PROJECT_SKILL_LIST_STATE,
  IMPLICIT_SKILL_LIST,
  PROJECT_SKILL_SORT_OPTIONS,
  ProjectSkillListState,
  REVIEW_USE_FILTER_OPTIONS,
  isProjectSkillSort,
} from './project-skill-list';
import { SkillPickerComponent } from './skill-picker.component';

type SkillMutation = 'add' | 'remove';

@Component({
  selector: 'app-implicit-skills',
  imports: [
    AsyncPipe,
    ListControlsComponent,
    ListFiltersComponent,
    ListRowComponent,
    PaginationComponent,
    SkillPickerComponent,
  ],
  templateUrl: './implicit-skills.component.html',
  styleUrls: [
    '../../policies/feature/policy-directory-list.page.css',
    '../../policies/ui/knowledge-list.css',
    '../../../shared/list-row/list-row-actions.css',
    './project-pages.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImplicitSkillsComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<SkillMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly sortOptions = PROJECT_SKILL_SORT_OPTIONS;
  protected readonly listState = signal<ProjectSkillListState>(
    DEFAULT_PROJECT_SKILL_LIST_STATE,
  );
  protected readonly filters = computed<readonly ListFilter[]>(() => [
    {
      kind: 'select',
      key: 'review',
      label: 'Review',
      value: this.listState().filter,
      options: REVIEW_USE_FILTER_OPTIONS,
    },
  ]);
  protected readonly page = signal(1);
  protected readonly skills = computed(() =>
    pageOf(
      selectClientList(this.project().skills, this.listState(), IMPLICIT_SKILL_LIST),
      this.page(),
      10,
    ),
  );
  protected readonly adding = signal(false);
  protected readonly confirmingSkillId = signal<string | null>(null);
  protected readonly nameError = signal<string | null>(null);
  protected readonly pickedSkill = signal<SkillListItem | null>(null);
  protected readonly linkedSkillIds = computed(() =>
    this.project().skills.map(skill => skill.skillId),
  );
  protected readonly mutation$ = mutationStates(this.mutations);

  protected search(search: string): void {
    this.changeList({ search });
  }

  protected filter(change: ListFilterChange): void {
    this.changeList({ filter: change.value });
  }

  protected sort(sortBy: string): void {
    if (isProjectSkillSort(sortBy)) this.changeList({ sortBy });
  }

  protected changeDirection(sortDirection: ListSortDirection): void {
    this.changeList({ sortDirection });
  }

  protected cancelAdding(): void {
    this.adding.set(false);
    this.nameError.set(null);
    this.pickedSkill.set(null);
  }

  protected addSkill(projectId: string, picker: SkillPickerComponent): void {
    const skill = this.pickedSkill();
    if (!skill) {
      this.nameError.set('Pick a skill from the search results.');
      return;
    }

    this.nameError.set(null);
    this.mutations.next({
      kind: 'add',
      request: this.projects.addProjectSkill(projectId, skill.id),
      done: () => {
        picker.clear();
        this.adding.set(false);
      },
    });
  }

  protected removeSkill(projectId: string, skillId: string): void {
    this.mutations.next({
      kind: 'remove',
      request: this.projects.removeProjectSkill(projectId, skillId),
      done: () => this.confirmingSkillId.set(null),
    });
  }

  private changeList(change: Partial<ProjectSkillListState>): void {
    this.listState.update(state => ({ ...state, ...change }));
    this.page.set(1);
  }
}
