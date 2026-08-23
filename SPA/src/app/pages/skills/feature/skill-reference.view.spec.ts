import { Skill } from '../data-access/skill.models';
import { createSkillReferenceView } from './skill-reference.view';

const skill: Skill = {
  id: 'skill-1',
  isDeleted: false,
  name: 'Angular code writer',
  description: 'Angular guidance',
  content: '# Skill',
  tags: [],
  referenceCount: 1,
  attachmentCount: 0,
  references: {
    'references/architecture.md': {
      content: '# Architecture\n\nObservable-first pages.',
      loadAutomatically: false,
    },
  },
  attachments: {},
};

describe('createSkillReferenceView', () => {
  it('selects a nested reference path and prepares its page content', () => {
    const view = createSkillReferenceView(
      skill,
      'references/architecture.md',
    );

    expect(view?.name).toBe('architecture.md');
    expect(view?.relativePath).toBe('references/architecture.md');
    expect(view?.blocks).toEqual([
      { kind: 'heading', level: 1, text: 'Architecture' },
      { kind: 'paragraph', text: 'Observable-first pages.' },
    ]);
  });

  it('returns undefined when the skill does not contain the reference', () => {
    expect(
      createSkillReferenceView(skill, 'references/missing.md'),
    ).toBeUndefined();
  });
});
