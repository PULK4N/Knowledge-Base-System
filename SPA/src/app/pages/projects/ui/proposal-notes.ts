import { ProposalNote } from '../../policies/data-access/policy.models';

export type ProposalNoteSort = 'updatedAt' | 'title';

/** Filters notes by title or content and sorts them by last update or title. */
export function selectProposalNotes(
  notes: readonly ProposalNote[],
  search: string,
  sort: ProposalNoteSort,
): readonly ProposalNote[] {
  const term = search.trim().toLowerCase();
  const matching = term
    ? notes.filter(
        note =>
          note.title.toLowerCase().includes(term) ||
          note.content.toLowerCase().includes(term),
      )
    : notes;

  return [...matching].sort((first, second) =>
    sort === 'title'
      ? first.title.localeCompare(second.title)
      : second.updatedAt.localeCompare(first.updatedAt),
  );
}
