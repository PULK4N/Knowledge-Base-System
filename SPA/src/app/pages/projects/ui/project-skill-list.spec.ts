import { selectClientList } from '../../../shared/list-state/list-state';
import { ProjectSkill, ReviewLevel } from '../../policies/data-access/policy.models';
import {
  DEFAULT_PROJECT_SKILL_LIST_STATE,
  IMPLICIT_SKILL_LIST,
  ProjectSkillListState,
  REVIEWER_LIST,
} from './project-skill-list';

const skill = (
  name: string,
  reviewLevel: ReviewLevel | null,
  useScenarios: readonly string[] = [],
  reviewNotes = '',
): ProjectSkill => ({
  skillId: name,
  name,
  useScenarios,
  usedForReview: reviewLevel !== null,
  reviewLevel,
  reviewNotes,
});

const skills = [
  skill('write-event-sourcing-modules', 'Heavy', ['Adding events']),
  skill('angular-code-writter', 'Easy', ['Writing Angular code'], 'Check signals'),
  skill('plannotator', null, ['Reviewing plans']),
];

const names = (
  definition: typeof IMPLICIT_SKILL_LIST,
  change: Partial<ProjectSkillListState>,
) =>
  selectClientList(
    skills,
    { ...DEFAULT_PROJECT_SKILL_LIST_STATE, ...change },
    definition,
  ).map(item => item.name);

describe('project skill lists', () => {
  it.each([
    [{}, ['angular-code-writter', 'plannotator', 'write-event-sourcing-modules']],
    [{ sortBy: 'ReviewLevel' }, ['angular-code-writter', 'write-event-sourcing-modules', 'plannotator']],
    [{ sortDirection: 'Descending' }, ['write-event-sourcing-modules', 'plannotator', 'angular-code-writter']],
    [{ search: 'angular' }, ['angular-code-writter']],
    [{ search: 'reviewing plans' }, ['plannotator']],
    [{ filter: 'true' }, ['angular-code-writter', 'write-event-sourcing-modules']],
    [{ filter: 'false' }, ['plannotator']],
  ] as const)('implicit skills with %j', (change, expected) => {
    expect(names(IMPLICIT_SKILL_LIST, change)).toEqual(expected);
  });

  it.each([
    [{ filter: 'Heavy' }, ['write-event-sourcing-modules']],
    [{ search: 'signals' }, ['angular-code-writter']],
  ] as const)('reviewers with %j', (change, expected) => {
    expect(names(REVIEWER_LIST, change)).toEqual(expected);
  });
});
