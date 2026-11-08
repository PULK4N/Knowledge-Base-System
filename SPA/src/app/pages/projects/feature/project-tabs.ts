export type ProjectTab =
  | 'overview'
  | 'policies'
  | 'implicit-skills'
  | 'reviewers'
  | 'features'
  | 'proposal-notes';

export const PROJECT_TABS: readonly { readonly id: ProjectTab; readonly label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'policies', label: 'Policies' },
  { id: 'implicit-skills', label: 'Implicit skills' },
  { id: 'reviewers', label: 'Reviewers' },
  { id: 'features', label: 'Features' },
  { id: 'proposal-notes', label: 'Proposal notes' },
];

/** Unknown or missing tab names open the Overview tab. */
export function parseProjectTab(value: string | null): ProjectTab {
  return PROJECT_TABS.find(tab => tab.id === value)?.id ?? 'overview';
}
