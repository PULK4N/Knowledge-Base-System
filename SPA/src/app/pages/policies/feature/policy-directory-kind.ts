export type DirectoryKind = 'topics' | 'agent-families' | 'projects';

const DIRECTORY_KINDS: readonly DirectoryKind[] = [
  'topics',
  'agent-families',
  'projects',
];

export function directoryKindFromRoute(kind: unknown): DirectoryKind {
  return DIRECTORY_KINDS.find(candidate => candidate === kind) ?? 'topics';
}
