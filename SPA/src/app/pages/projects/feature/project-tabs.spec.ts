import { parseProjectTab } from './project-tabs';

describe('parseProjectTab', () => {
  it('opens known tabs and falls back to Overview', () => {
    expect(parseProjectTab('reviewers')).toBe('reviewers');
    expect(parseProjectTab('proposal-notes')).toBe('proposal-notes');
    expect(parseProjectTab(null)).toBe('overview');
    expect(parseProjectTab('unknown')).toBe('overview');
  });
});
