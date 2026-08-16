import { parseSkillTab } from './skill-tabs';

describe('parseSkillTab', () => {
  it.each([
    ['content', 'content'],
    ['references', 'references'],
    ['attachments', 'attachments'],
    [null, 'content'],
    ['unknown', 'content'],
  ] as const)('maps %s to %s', (value, expected) => {
    expect(parseSkillTab(value)).toBe(expected);
  });
});
