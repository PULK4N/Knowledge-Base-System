import { Agent } from '../data-access/agents.models';
import { AgentsEvent, AgentsState, initialAgentsState, reduceAgents } from './agents-state';

const codex: Agent = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userName: 'codex',
  displayName: 'Codex',
  isAdministrator: false,
  permissions: ['memory.write'],
};

function reduce(...events: AgentsEvent[]): AgentsState {
  return events.reduce(reduceAgents, initialAgentsState);
}

describe('reduceAgents', () => {
  it.each([
    [[{ type: 'create-opened' }], { editor: { mode: 'create' }, command: { status: 'idle' } }],
    [
      [{ type: 'edit-opened', agent: codex }],
      { editor: { mode: 'edit', agent: codex }, command: { status: 'idle' } },
    ],
    [
      [{ type: 'create-opened' }, { type: 'submitted' }],
      { editor: { mode: 'create' }, command: { status: 'saving' } },
    ],
    [
      [{ type: 'create-opened' }, { type: 'submitted' }, { type: 'created', agent: codex }],
      { editor: { mode: 'none' }, command: { status: 'saved', message: 'Agent codex was created.' } },
    ],
    [
      [{ type: 'edit-opened', agent: codex }, { type: 'submitted' }, { type: 'permissions-saved', agent: codex }],
      { editor: { mode: 'none' }, command: { status: 'saved', message: 'Permissions of codex were saved.' } },
    ],
    [
      [{ type: 'create-opened' }, { type: 'submitted' }, { type: 'failed', message: 'Taken' }],
      { editor: { mode: 'create' }, command: { status: 'error', message: 'Taken' } },
    ],
    [
      [{ type: 'create-opened' }, { type: 'submitted' }, { type: 'failed', message: 'Taken' }, { type: 'editor-closed' }],
      initialAgentsState,
    ],
    [
      [{ type: 'create-opened' }, { type: 'submitted' }, { type: 'created', agent: codex }, { type: 'edit-opened', agent: codex }],
      { editor: { mode: 'edit', agent: codex }, command: { status: 'idle' } },
    ],
  ] as [AgentsEvent[], AgentsState][])('moves through %j to %j', (events, expected) => {
    expect(reduce(...events)).toEqual(expected);
  });
});
