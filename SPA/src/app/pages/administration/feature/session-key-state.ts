import { Agent } from '../data-access/agents.models';

export type SessionKeyState =
  | { readonly status: 'closed' }
  | { readonly status: 'generating'; readonly agent: Agent }
  | { readonly status: 'shown'; readonly agent: Agent; readonly sessionKey: string }
  | { readonly status: 'error'; readonly agent: Agent; readonly message: string };

export type SessionKeyEvent =
  | { readonly type: 'requested'; readonly agent: Agent }
  | { readonly type: 'generated'; readonly sessionKey: string }
  | { readonly type: 'failed'; readonly message: string }
  | { readonly type: 'closed' };

export const closedSessionKey: SessionKeyState = { status: 'closed' };

export function reduceSessionKey(state: SessionKeyState, event: SessionKeyEvent): SessionKeyState {
  switch (event.type) {
    case 'requested':
      return { status: 'generating', agent: event.agent };
    case 'generated':
      return state.status === 'generating'
        ? { status: 'shown', agent: state.agent, sessionKey: event.sessionKey }
        : state;
    case 'failed':
      return state.status === 'generating'
        ? { status: 'error', agent: state.agent, message: event.message }
        : state;
    case 'closed':
      return closedSessionKey;
  }
}
