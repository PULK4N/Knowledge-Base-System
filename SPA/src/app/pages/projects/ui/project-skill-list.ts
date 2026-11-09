import { ListControlOption } from '../../../shared/list-controls/list-controls.component';
import {
  ClientListDefinition,
  ClientListState,
  compareText,
} from '../../../shared/list-state/list-state';
import {
  ProjectSkill,
  REVIEW_LEVELS,
} from '../../policies/data-access/policy.models';

export type ProjectSkillSort = 'Name' | 'ReviewLevel';

/** Implicit skills filter by review use ('true' / 'false'), reviewers by level; '' keeps all. */
export type ProjectSkillListState = ClientListState<ProjectSkillSort, string>;

export const PROJECT_SKILL_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'Name', label: 'Name' },
  { value: 'ReviewLevel', label: 'Review level' },
];

export const DEFAULT_PROJECT_SKILL_LIST_STATE: ProjectSkillListState = {
  search: '',
  filter: '',
  sortBy: 'Name',
  sortDirection: 'Ascending',
};

export const REVIEW_USE_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: '', label: 'Any' },
  { value: 'true', label: 'Used for review' },
  { value: 'false', label: 'Not used for review' },
];

export const REVIEW_LEVEL_FILTER_OPTIONS: readonly ListControlOption[] = [
  { value: '', label: 'Any' },
  ...REVIEW_LEVELS.map(level => ({ value: level, label: level })),
];

export function isProjectSkillSort(value: string): value is ProjectSkillSort {
  return PROJECT_SKILL_SORT_OPTIONS.some(option => option.value === value);
}

/** Skills without a level sort after every level. */
function levelRank(skill: ProjectSkill): number {
  return skill.reviewLevel === null
    ? REVIEW_LEVELS.length
    : REVIEW_LEVELS.indexOf(skill.reviewLevel);
}

function compareProjectSkills(
  left: ProjectSkill,
  right: ProjectSkill,
  sortBy: ProjectSkillSort,
): number {
  const byName = compareText(left.name, right.name);

  return sortBy === 'ReviewLevel'
    ? levelRank(left) - levelRank(right) || byName
    : byName;
}

export const IMPLICIT_SKILL_LIST: ClientListDefinition<
  ProjectSkill,
  ProjectSkillSort,
  string
> = {
  searchText: skill => [skill.name, ...skill.useScenarios].join(' '),
  matchesFilter: (skill, filter) =>
    filter === '' || String(skill.usedForReview) === filter,
  compare: compareProjectSkills,
};

export const REVIEWER_LIST: ClientListDefinition<
  ProjectSkill,
  ProjectSkillSort,
  string
> = {
  searchText: skill =>
    [skill.name, skill.reviewNotes, ...skill.useScenarios].join(' '),
  matchesFilter: (skill, filter) =>
    filter === '' || skill.reviewLevel === filter,
  compare: compareProjectSkills,
};
