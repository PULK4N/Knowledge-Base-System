import { toReviewLevelModels } from './project-review';

describe('toReviewLevelModels', () => {
  it('keeps complete levels, trims them and leaves empty levels out', () => {
    expect(
      toReviewLevelModels({
        EasyModel: ' gpt-6-luna ',
        EasyReasoning: 'low',
        MediumModel: '',
        MediumReasoning: '',
        HeavyModel: 'gpt-6.1-sol',
        HeavyReasoning: 'high',
      }),
    ).toEqual({
      status: 'valid',
      levelModels: {
        Easy: { model: 'gpt-6-luna', reasoning: 'low' },
        Heavy: { model: 'gpt-6.1-sol', reasoning: 'high' },
      },
    });
  });

  it('rejects a level with only a model or only a reasoning', () => {
    expect(
      toReviewLevelModels({ MediumModel: 'gpt-6.1-sol', HeavyReasoning: 'high' }),
    ).toEqual({
      status: 'invalid',
      message: 'Give both a model and a reasoning for: Medium, Heavy.',
    });
  });
});
