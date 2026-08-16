import { Skill } from '../data-access/skill.models';
import { MarkdownBlock, parseMarkdownBlocks } from '../ui/markdown-blocks';

export interface SkillReferenceView {
  readonly skill: Skill;
  readonly relativePath: string;
  readonly name: string;
  readonly blocks: readonly MarkdownBlock[];
}

export function createSkillReferenceView(
  skill: Skill,
  relativePath: string,
): SkillReferenceView | undefined {
  const reference = skill.references[relativePath];
  if (!reference) return undefined;

  const pathParts = relativePath.split('/').filter(Boolean);

  return {
    skill,
    relativePath,
    name: pathParts.at(-1) ?? relativePath,
    blocks: parseMarkdownBlocks(reference.content),
  };
}
