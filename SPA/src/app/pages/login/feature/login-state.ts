import { LoginResult } from '../../../core/auth/auth.models';

export type LoginState =
  | {
      readonly step: 'credentials';
      readonly status: 'idle' | 'submitting' | 'rejected' | 'error';
      readonly message: string | null;
    }
  | {
      readonly step: 'new-password';
      /** Sent again with the new password, because no session exists yet. */
      readonly currentPassword: string;
      readonly status: 'idle' | 'submitting' | 'error';
      readonly message: string | null;
    }
  | { readonly step: 'done' };

export type LoginEvent =
  | { readonly type: 'submitted' }
  | {
      readonly type: 'login-completed';
      readonly userName: string;
      readonly password: string;
      readonly result: LoginResult;
    }
  | { readonly type: 'new-password-saved' }
  | { readonly type: 'failed'; readonly message: string };

export const initialLoginState: LoginState = {
  step: 'credentials',
  status: 'idle',
  message: null,
};

export function reduceLogin(state: LoginState, event: LoginEvent): LoginState {
  if (state.step === 'done') return state;

  switch (event.type) {
    case 'submitted':
      return { ...state, status: 'submitting', message: null };
    case 'failed':
      return { ...state, status: 'error', message: event.message };
    case 'login-completed':
      if (!event.result.succeeded) {
        return {
          step: 'credentials',
          status: 'rejected',
          message: 'Wrong username or password.',
        };
      }
      return event.result.mustChangePassword
        ? {
            step: 'new-password',
            currentPassword: event.password,
            status: 'idle',
            message: null,
          }
        : { step: 'done' };
    case 'new-password-saved':
      return { step: 'done' };
  }
}
