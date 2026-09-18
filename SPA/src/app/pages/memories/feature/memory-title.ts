const MAXIMUM_TITLE_LENGTH = 110;
const DEFAULT_TITLE = 'Conversation memory';

export interface MemoryTitleSource {
  readonly sessionTitle: string;
  readonly summary: string;
}

export function memoryTitle(memory: MemoryTitleSource): string {
  const title =
    memory.sessionTitle.trim() || summaryFirstLine(memory.summary);

  if (!title) return DEFAULT_TITLE;

  return title.length > MAXIMUM_TITLE_LENGTH
    ? `${title.slice(0, MAXIMUM_TITLE_LENGTH).trimEnd()}…`
    : title;
}

function summaryFirstLine(summary: string): string {
  return (
    summary
      .split(/\r?\n/)
      .map(line => line.trim())
      .find(Boolean)
      ?.replace(/^#{1,6}\s+/, '')
      .replace(/^[-*]\s+/, '') ?? ''
  );
}
