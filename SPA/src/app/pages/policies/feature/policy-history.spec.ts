import {
  describePolicyEvent,
  policyHistoryLink,
  toPolicyHistory,
} from './policy-history';

describe('describePolicyEvent', () => {
  it.each([
    ['GeneralPolicyAddedV2', 'Policy added'],
    ['TopicPolicyUpdatedV2', 'Policy updated'],
    ['AgentFamilyPolicyRemovedV2', 'Policy removed'],
    ['ProjectPolicyUpdatedV2', 'Policy updated'],
  ])('describes %s as %s', (eventName, expected) => {
    expect(describePolicyEvent(eventName)).toBe(expected);
  });
});

describe('toPolicyHistory', () => {
  it('lists the newest change first and hides the memory of web app changes', () => {
    expect(
      toPolicyHistory([
        {
          eventName: 'GeneralPolicyAddedV2',
          timestamp: '2026-09-23T08:00:00Z',
          memoryId: 'memory-1',
          isUserOriginated: false,
        },
        {
          eventName: 'GeneralPolicyUpdatedV2',
          timestamp: '2026-09-23T09:00:00Z',
          memoryId: 'user-memory',
          isUserOriginated: true,
        },
      ]),
    ).toEqual([
      {
        key: '1',
        action: 'Policy updated',
        timestamp: '2026-09-23T09:00:00Z',
        memoryId: null,
      },
      {
        key: '0',
        action: 'Policy added',
        timestamp: '2026-09-23T08:00:00Z',
        memoryId: 'memory-1',
      },
    ]);
  });
});

describe('policyHistoryLink', () => {
  it.each([
    [{ kind: 'general' } as const, '/policies/general/policies/p-1/history'],
    [
      { kind: 'topic', topicName: 'Angular' } as const,
      '/policies/topics/Angular/policies/p-1/history',
    ],
    [
      { kind: 'agentFamily', agentFamilyName: 'claude' } as const,
      '/policies/agent-families/claude/policies/p-1/history',
    ],
    [
      { kind: 'project', projectId: 'project-1' } as const,
      '/policies/projects/project-1/policies/p-1/history',
    ],
  ])('links a %o policy to its history page', (scope, expected) => {
    expect(policyHistoryLink(scope, 'p-1').join('/').replace('//', '/')).toBe(
      expected,
    );
  });
});
