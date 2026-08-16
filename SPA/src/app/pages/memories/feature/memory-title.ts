const MAXIMUM_TITLE_LENGTH = 110;

export function memoryTitle(summary: string): string {
  const firstLine = summary
    .split(/\r?\n/)
    .map(line => line.trim())
    .find(Boolean)
    ?.replace(/^#{1,6}\s+/, '')
    .replace(/^[-*]\s+/, '');

  if (!firstLine) return 'Conversation memory';

  return firstLine.length > MAXIMUM_TITLE_LENGTH
    ? `${firstLine.slice(0, MAXIMUM_TITLE_LENGTH).trimEnd()}…`
    : firstLine;
}
