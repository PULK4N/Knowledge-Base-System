export type SkillTab = 'content' | 'references' | 'attachments' | 'history';

const SKILL_TABS: readonly SkillTab[] = [
  'content',
  'references',
  'attachments',
  'history',
];

export function parseSkillTab(value: string | null): SkillTab {
  return SKILL_TABS.find(tab => tab === value) ?? 'content';
}
