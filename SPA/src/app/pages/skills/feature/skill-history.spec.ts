import { describeSkillEvent, toSkillHistory } from './skill-history';

describe('describeSkillEvent', () => {
  it.each([
    ['SkillCreatedV3', 'Skill created'],
    ['SkillDeletedV2', 'Skill deleted'],
    ['SkillDetailsUpdatedV2', 'Details updated'],
    ['SkillReferenceAutoLoadUpdatedV2', 'Reference auto load updated'],
    ['SkillAttachmentAddedV2', 'Attachment added'],
  ])('describes %s as %s', (eventName, expected) => {
    expect(describeSkillEvent(eventName)).toBe(expected);
  });
});

describe('toSkillHistory', () => {
  it('lists the newest change first and hides the memory of web app changes', () => {
    expect(
      toSkillHistory([
        {
          eventName: 'SkillCreatedV3',
          timestamp: '2026-09-23T08:00:00Z',
          memoryId: 'memory-1',
          isUserOriginated: false,
        },
        {
          eventName: 'SkillDetailsUpdatedV2',
          timestamp: '2026-09-23T09:00:00Z',
          memoryId: 'user-memory',
          isUserOriginated: true,
        },
      ]),
    ).toEqual([
      {
        key: '1',
        action: 'Details updated',
        timestamp: '2026-09-23T09:00:00Z',
        memoryId: null,
      },
      {
        key: '0',
        action: 'Skill created',
        timestamp: '2026-09-23T08:00:00Z',
        memoryId: 'memory-1',
      },
    ]);
  });
});
