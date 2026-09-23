import {
  PolicyMemoryHistoryEntry,
  PolicyScope,
} from '../data-access/policy.models';

export interface PolicyHistoryItem {
  readonly key: string;
  readonly action: string;
  readonly timestamp: string;
  /** Memory whose chat made the change, or null for changes made in the web app. */
  readonly memoryId: string | null;
}

export function toPolicyHistory(
  entries: readonly PolicyMemoryHistoryEntry[],
): readonly PolicyHistoryItem[] {
  return entries
    .map((entry, index) => ({
      key: `${index}`,
      action: describePolicyEvent(entry.eventName),
      timestamp: entry.timestamp,
      memoryId: entry.isUserOriginated ? null : entry.memoryId,
    }))
    .reverse();
}

/** Turns an event name such as `TopicPolicyUpdatedV2` into `Policy updated`. */
export function describePolicyEvent(eventName: string): string {
  const words = eventName
    .replace(/V\d+$/, '')
    .split(/(?=[A-Z])/)
    .filter(word => word.length > 0);
  const policyIndex = words.indexOf('Policy');
  const shownWords = policyIndex > 0 ? words.slice(policyIndex) : words;
  const sentence = shownWords.join(' ').toLowerCase();

  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}

/** Router link of the policy list that owns a scope. */
export function policyListLink(scope: PolicyScope): readonly string[] {
  switch (scope.kind) {
    case 'general':
      return ['/policies', 'general'];
    case 'topic':
      return ['/policies', 'topics', scope.topicName];
    case 'agentFamily':
      return ['/policies', 'agent-families', scope.agentFamilyName];
    case 'project':
      return ['/policies', 'projects', scope.projectId];
  }
}

export function policyHistoryLink(
  scope: PolicyScope,
  policyId: string,
): readonly string[] {
  return [...policyListLink(scope), 'policies', policyId, 'history'];
}
