import {
  REVIEW_LEVELS,
  ReviewLevel,
  ReviewLevelModels,
  ReviewModel,
} from '../../policies/data-access/policy.models';

export type ReviewLevelModelsResult =
  | { readonly status: 'valid'; readonly levelModels: ReviewLevelModels }
  | { readonly status: 'invalid'; readonly message: string };

/**
 * Builds the level to model mapping from the form fields `<Level>Model` and
 * `<Level>Reasoning`. A level with both fields empty is left out; a level with
 * only one filled is rejected.
 */
export function toReviewLevelModels(
  form: Readonly<Record<string, string | undefined>>,
): ReviewLevelModelsResult {
  const levelModels: Partial<Record<ReviewLevel, ReviewModel>> = {};
  const incomplete: ReviewLevel[] = [];

  for (const level of REVIEW_LEVELS) {
    const model = (form[`${level}Model`] ?? '').trim();
    const reasoning = (form[`${level}Reasoning`] ?? '').trim();

    if (model && reasoning) levelModels[level] = { model, reasoning };
    else if (model || reasoning) incomplete.push(level);
  }

  return incomplete.length > 0
    ? {
        status: 'invalid',
        message: `Give both a model and a reasoning for: ${incomplete.join(', ')}.`,
      }
    : { status: 'valid', levelModels };
}
