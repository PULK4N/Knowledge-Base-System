export type ChangePasswordState =
  | { readonly status: 'closed' | 'editing' | 'saving' | 'saved' }
  | { readonly status: 'error'; readonly message: string };

export type ChangePasswordEvent =
  | { readonly type: 'opened' | 'closed' | 'submitted' | 'saved' }
  | { readonly type: 'failed'; readonly message: string };

export const closedChangePassword: ChangePasswordState = { status: 'closed' };

export function reduceChangePassword(
  state: ChangePasswordState,
  event: ChangePasswordEvent,
): ChangePasswordState {
  switch (event.type) {
    case 'opened':
      return { status: 'editing' };
    case 'closed':
      return closedChangePassword;
    case 'submitted':
      return state.status === 'closed' ? state : { status: 'saving' };
    case 'saved':
      return state.status === 'saving' ? { status: 'saved' } : state;
    case 'failed':
      return state.status === 'saving'
        ? { status: 'error', message: event.message }
        : state;
  }
}
