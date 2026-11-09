import { ProposalNote } from '../../policies/data-access/policy.models';
import { selectProposalNotes } from './proposal-notes';

const note = (title: string, content: string, updatedAt: string): ProposalNote => ({
  noteId: title,
  title,
  content,
  createdAt: '2026-11-01T00:00:00Z',
  updatedAt,
});

describe('selectProposalNotes', () => {
  const notes = [
    note('Skill visibility', 'Global or linked projects only', '2026-11-02T00:00:00Z'),
    note('Retire tags', 'Use scenarios instead', '2026-11-05T00:00:00Z'),
    note('Archive projects', 'Hide old projects', '2026-11-03T00:00:00Z'),
  ];

  it('sorts by last update, newest first, or by title', () => {
    expect(selectProposalNotes(notes, '', 'updatedAt').map(item => item.title)).toEqual([
      'Retire tags',
      'Archive projects',
      'Skill visibility',
    ]);
    expect(selectProposalNotes(notes, '', 'title').map(item => item.title)).toEqual([
      'Archive projects',
      'Retire tags',
      'Skill visibility',
    ]);
  });

  it.each([
    ['title', 'Descending', ['Skill visibility', 'Retire tags', 'Archive projects']],
    ['updatedAt', 'Ascending', ['Skill visibility', 'Archive projects', 'Retire tags']],
  ] as const)('sorts by %s %s', (sort, direction, expected) => {
    expect(selectProposalNotes(notes, '', sort, direction).map(item => item.title)).toEqual(
      expected,
    );
  });

  it('matches the search in the title or content, ignoring case', () => {
    expect(selectProposalNotes(notes, 'SCENARIOS', 'title').map(item => item.title)).toEqual([
      'Retire tags',
    ]);
  });
});
