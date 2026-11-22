import {
  ChangePasswordEvent,
  ChangePasswordState,
  closedChangePassword,
  reduceChangePassword,
} from './change-password-state';

function reduce(...events: ChangePasswordEvent[]): ChangePasswordState {
  return events.reduce(reduceChangePassword, closedChangePassword);
}

const failed: ChangePasswordEvent = { type: 'failed', message: 'Wrong password' };

describe('reduceChangePassword', () => {
  it.each([
    [[{ type: 'opened' }], { status: 'editing' }],
    [[{ type: 'opened' }, { type: 'submitted' }], { status: 'saving' }],
    [[{ type: 'opened' }, { type: 'submitted' }, { type: 'saved' }], { status: 'saved' }],
    [[{ type: 'opened' }, { type: 'submitted' }, failed], { status: 'error', message: 'Wrong password' }],
    [[{ type: 'opened' }, { type: 'submitted' }, failed, { type: 'submitted' }], { status: 'saving' }],
    [[{ type: 'opened' }, { type: 'submitted' }, { type: 'closed' }], { status: 'closed' }],
    [[{ type: 'opened' }, { type: 'submitted' }, { type: 'saved' }, { type: 'opened' }], { status: 'editing' }],
  ] as [ChangePasswordEvent[], ChangePasswordState][])('moves through %j to %j', (events, expected) => {
    expect(reduce(...events)).toEqual(expected);
  });

  it.each([
    [[{ type: 'submitted' }]],
    [[{ type: 'saved' }]],
    [[failed]],
    [[{ type: 'opened' }, { type: 'submitted' }, { type: 'closed' }, { type: 'saved' }]],
  ] as [ChangePasswordEvent[]][])('ignores %j while the dialog is closed', events => {
    expect(reduce(...events)).toEqual({ status: 'closed' });
  });
});
