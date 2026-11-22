import { Agent } from '../data-access/agents.models';
import { SessionKeyEvent, SessionKeyState, closedSessionKey, reduceSessionKey } from './session-key-state';

const codex: Agent = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userName: 'codex',
  displayName: 'Codex',
  isAdministrator: false,
  permissions: ['skills.read'],
};

function reduce(...events: SessionKeyEvent[]): SessionKeyState {
  return events.reduce(reduceSessionKey, closedSessionKey);
}

describe('reduceSessionKey', () => {
  it('shows the generated key for the requested agent', () => {
    expect(reduce({ type: 'requested', agent: codex }, { type: 'generated', sessionKey: 'key' })).toEqual({
      status: 'shown',
      agent: codex,
      sessionKey: 'key',
    });
  });

  it('shows the error when generating fails', () => {
    expect(reduce({ type: 'requested', agent: codex }, { type: 'failed', message: 'No.' })).toEqual({
      status: 'error',
      agent: codex,
      message: 'No.',
    });
  });

  it('forgets the key when closed', () => {
    expect(
      reduce({ type: 'requested', agent: codex }, { type: 'generated', sessionKey: 'key' }, { type: 'closed' }),
    ).toEqual(closedSessionKey);
  });

  it.each([
    [{ type: 'generated', sessionKey: 'key' }],
    [{ type: 'failed', message: 'No.' }],
  ] as [SessionKeyEvent][])('ignores %j after the dialog was closed', event => {
    expect(reduce({ type: 'requested', agent: codex }, { type: 'closed' }, event)).toEqual(closedSessionKey);
  });
});
