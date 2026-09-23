import { SkillMemoryHistoryEntry } from '../data-access/skill.models';

export interface SkillHistoryItem {
  readonly key: string;
  readonly action: string;
  readonly timestamp: string;
  /** Memory whose chat made the change, or null for changes made in the web app. */
  readonly memoryId: string | null;
}

export function toSkillHistory(
  entries: readonly SkillMemoryHistoryEntry[],
): readonly SkillHistoryItem[] {
  return entries
    .map((entry, index) => ({
      key: `${index}`,
      action: describeSkillEvent(entry.eventName),
      timestamp: entry.timestamp,
      memoryId: entry.isUserOriginated ? null : entry.memoryId,
    }))
    .reverse();
}

/** Turns an event name such as `SkillReferenceAddedV1` into `Reference added`. */
export function describeSkillEvent(eventName: string): string {
  const words = eventName
    .replace(/V\d+$/, '')
    .split(/(?=[A-Z])/)
    .filter(word => word.length > 0);
  const shownWords =
    words.length > 2 && words[0] === 'Skill' ? words.slice(1) : words;
  const sentence = shownWords.join(' ').toLowerCase();

  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}
