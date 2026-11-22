import { Agent } from '../data-access/agents.models';

export type AgentEditor =
  | { readonly mode: 'none' }
  | { readonly mode: 'create' }
  | { readonly mode: 'edit'; readonly agent: Agent };

export type AgentsCommandState =
  | { readonly status: 'idle' | 'saving' }
  | { readonly status: 'saved'; readonly message: string }
  | { readonly status: 'error'; readonly message: string };

export interface AgentsState {
  readonly editor: AgentEditor;
  readonly command: AgentsCommandState;
}

export type AgentsEvent =
  | { readonly type: 'create-opened' }
  | { readonly type: 'edit-opened'; readonly agent: Agent }
  | { readonly type: 'editor-closed' }
  | { readonly type: 'submitted' }
  | { readonly type: 'created' | 'permissions-saved'; readonly agent: Agent }
  | { readonly type: 'failed'; readonly message: string };

export const initialAgentsState: AgentsState = {
  editor: { mode: 'none' },
  command: { status: 'idle' },
};

export function reduceAgents(state: AgentsState, event: AgentsEvent): AgentsState {
  switch (event.type) {
    case 'create-opened':
      return { editor: { mode: 'create' }, command: { status: 'idle' } };
    case 'edit-opened':
      return { editor: { mode: 'edit', agent: event.agent }, command: { status: 'idle' } };
    case 'editor-closed':
      return initialAgentsState;
    case 'submitted':
      return { ...state, command: { status: 'saving' } };
    case 'created':
      return {
        editor: { mode: 'none' },
        command: { status: 'saved', message: `Agent ${event.agent.userName} was created.` },
      };
    case 'permissions-saved':
      return {
        editor: { mode: 'none' },
        command: {
          status: 'saved',
          message: `Permissions of ${event.agent.userName} were saved.`,
        },
      };
    case 'failed':
      return { ...state, command: { status: 'error', message: event.message } };
  }
}
