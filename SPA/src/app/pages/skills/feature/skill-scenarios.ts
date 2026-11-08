/** Pure edits of a use-scenario draft; each returns a new list. */
export function moveScenario(
  scenarios: readonly string[],
  index: number,
  offset: -1 | 1,
): readonly string[] {
  const target = index + offset;
  if (target < 0 || target >= scenarios.length) return scenarios;

  const moved = [...scenarios];
  [moved[index], moved[target]] = [moved[target], moved[index]];
  return moved;
}

export function replaceScenario(
  scenarios: readonly string[],
  index: number,
  value: string,
): readonly string[] {
  return scenarios.map((scenario, current) =>
    current === index ? value : scenario,
  );
}

export function removeScenario(
  scenarios: readonly string[],
  index: number,
): readonly string[] {
  return scenarios.filter((_, current) => current !== index);
}

/** Trims every line; a draft with a blank line cannot be saved. */
export function toSavedScenarios(
  scenarios: readonly string[],
): readonly string[] | null {
  const trimmed = scenarios.map(scenario => scenario.trim());
  return trimmed.some(scenario => scenario.length === 0) ? null : trimmed;
}
