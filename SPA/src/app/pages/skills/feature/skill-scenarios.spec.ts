import {
  moveScenario,
  removeScenario,
  replaceScenario,
  toSavedScenarios,
} from './skill-scenarios';

describe('skill scenario edits', () => {
  const scenarios = ['Writing code', 'Reviewing code', 'Planning'];

  it('moves a scenario and keeps the list when the move leaves the bounds', () => {
    expect(moveScenario(scenarios, 1, -1)).toEqual([
      'Reviewing code',
      'Writing code',
      'Planning',
    ]);
    expect(moveScenario(scenarios, 0, -1)).toBe(scenarios);
    expect(moveScenario(scenarios, 2, 1)).toBe(scenarios);
  });

  it('replaces and removes one scenario without changing the input', () => {
    expect(replaceScenario(scenarios, 2, 'Testing')).toEqual([
      'Writing code',
      'Reviewing code',
      'Testing',
    ]);
    expect(removeScenario(scenarios, 0)).toEqual(['Reviewing code', 'Planning']);
    expect(scenarios).toEqual(['Writing code', 'Reviewing code', 'Planning']);
  });

  it('trims saved scenarios and refuses a blank line', () => {
    expect(toSavedScenarios(['  Writing code ', 'Planning'])).toEqual([
      'Writing code',
      'Planning',
    ]);
    expect(toSavedScenarios(['Writing code', '   '])).toBeNull();
    expect(toSavedScenarios([])).toEqual([]);
  });
});
