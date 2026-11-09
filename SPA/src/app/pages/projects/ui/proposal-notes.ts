import { ListSortDirection } from '../../../shared/list-state/list-state';
import { ProposalNote } from '../../policies/data-access/policy.models';

export type ProposalNoteSort = 'updatedAt' | 'title';

/**
 * Filters notes by title or content and sorts them by last update or title.
 * Without a direction, last update sorts newest first and title A to Z.
 */
export function selectProposalNotes(
  notes: readonly ProposalNote[],
  search: string,
  sort: ProposalNoteSort,
  direction: ListSortDirection = sort === 'title' ? 'Ascending' : 'Descending',
): readonly ProposalNote[] {
  const term = search.trim().toLowerCase();
  const matching = term
    ? notes.filter(
        note =>
          note.title.toLowerCase().includes(term) ||
          note.content.toLowerCase().includes(term),
      )
    : notes;
  const factor = direction === 'Descending' ? -1 : 1;

  return [...matching].sort(
    (first, second) =>
      factor *
      (sort === 'title'
        ? first.title.localeCompare(second.title)
        : first.updatedAt.localeCompare(second.updatedAt)),
  );
}
