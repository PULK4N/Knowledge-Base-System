import { memoryTitle } from './memory-title';

describe('memoryTitle', () => {
  it.each([
    ['# Event sourcing review\nMore details', 'Event sourcing review'],
    ['- Prefer focused tests', 'Prefer focused tests'],
    ['  \n\n', 'Conversation memory'],
  ])('derives a readable list title from %j', (summary, expected) => {
    expect(memoryTitle(summary)).toBe(expected);
  });

  it('shortens very long first lines', () => {
    expect(memoryTitle('A'.repeat(140))).toBe(`${'A'.repeat(110)}…`);
  });
});
