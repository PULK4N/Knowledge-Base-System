export type SkillTab = 'content' | 'references' | 'attachments';

const SKILL_TABS: readonly SkillTab[] = [
  'content',
  'references',
  'attachments',
];

export function parseSkillTab(value: string | null): SkillTab {
  return SKILL_TABS.find(tab => tab === value) ?? 'content';
}
