import { describeFeatureEvent, toFeatureHistory } from './feature-history';

describe('describeFeatureEvent', () => {
  it.each([
    ['FeatureAddedV2', 'Feature added'],
    ['FeatureRemovedV2', 'Feature removed'],
    ['FeaturePlanChangedV2', 'Plan changed'],
    ['FeatureResearchDiscoveryUpdatedV3', 'Research discovery updated'],
    ['FeatureStatusUpdatedV2', 'Status updated'],
  ])('describes %s as %s', (eventName, expected) => {
    expect(describeFeatureEvent(eventName)).toBe(expected);
  });
});

describe('toFeatureHistory', () => {
  it('lists the newest change first and hides the memory of web app changes', () => {
    expect(
      toFeatureHistory([
        {
          eventName: 'FeatureAddedV2',
          timestamp: '2026-09-23T08:00:00Z',
          memoryId: 'memory-1',
          isUserOriginated: false,
        },
        {
          eventName: 'FeatureSummaryUpdatedV2',
          timestamp: '2026-09-23T09:00:00Z',
          memoryId: 'user-memory',
          isUserOriginated: true,
        },
      ]),
    ).toEqual([
      {
        key: '1',
        action: 'Summary updated',
        timestamp: '2026-09-23T09:00:00Z',
        memoryId: null,
      },
      {
        key: '0',
        action: 'Feature added',
        timestamp: '2026-09-23T08:00:00Z',
        memoryId: 'memory-1',
      },
    ]);
  });
});
