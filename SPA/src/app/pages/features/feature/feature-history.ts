import { FeatureMemoryHistoryEntry } from '../data-access/feature.models';

export interface FeatureHistoryItem {
  readonly key: string;
  readonly action: string;
  readonly timestamp: string;
  /** Memory whose chat made the change, or null for changes made in the web app. */
  readonly memoryId: string | null;
}

export function toFeatureHistory(
  entries: readonly FeatureMemoryHistoryEntry[],
): readonly FeatureHistoryItem[] {
  return entries
    .map((entry, index) => ({
      key: `${index}`,
      action: describeFeatureEvent(entry.eventName),
      timestamp: entry.timestamp,
      memoryId: entry.isUserOriginated ? null : entry.memoryId,
    }))
    .reverse();
}

/** Turns an event name such as `FeaturePlanAddedV2` into `Plan added`. */
export function describeFeatureEvent(eventName: string): string {
  const words = eventName
    .replace(/V\d+$/, '')
    .split(/(?=[A-Z])/)
    .filter(word => word.length > 0);
  const shownWords =
    words.length > 2 && words[0] === 'Feature' ? words.slice(1) : words;
  const sentence = shownWords.join(' ').toLowerCase();

  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}
