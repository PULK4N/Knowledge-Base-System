import { LoginEvent, LoginState, initialLoginState, reduceLogin } from './login-state';

function reduce(...events: LoginEvent[]): LoginState {
  return events.reduce(reduceLogin, initialLoginState);
}

const submitted: LoginEvent = { type: 'submitted' };

function loginCompleted(succeeded: boolean, mustChangePassword: boolean): LoginEvent {
  return {
    type: 'login-completed',
    userName: ' admin ',
    password: '123',
    result: { succeeded, mustChangePassword },
  };
}

describe('reduceLogin', () => {
  it('marks a submitted login as submitting and clears the last message', () => {
    expect(reduce(loginCompleted(false, false), submitted)).toEqual({
      step: 'credentials',
      status: 'submitting',
      message: null,
    });
  });

  it('rejects wrong credentials on the credentials step', () => {
    expect(reduce(submitted, loginCompleted(false, false))).toEqual({
      step: 'credentials',
      status: 'rejected',
      message: 'Wrong username or password.',
    });
  });

  it('finishes a login that needs no password change', () => {
    expect(reduce(submitted, loginCompleted(true, false))).toEqual({ step: 'done' });
  });

  it('asks for a new password and keeps the current one for the next request', () => {
    expect(reduce(submitted, loginCompleted(true, true))).toEqual({
      step: 'new-password',
      currentPassword: '123',
      status: 'idle',
      message: null,
    });
  });

  it.each([
    [[] as LoginEvent[], 'credentials'],
    [[submitted, loginCompleted(true, true)], 'new-password'],
  ])('keeps the step and shows the message when a request fails', (before, step) => {
    const state = reduce(...before, submitted, { type: 'failed', message: 'Unavailable' });

    expect(state).toMatchObject({ step, status: 'error', message: 'Unavailable' });
  });

  it('finishes after the new password is saved and ignores later events', () => {
    const done = reduce(submitted, loginCompleted(true, true), submitted, {
      type: 'new-password-saved',
    });

    expect(done).toEqual({ step: 'done' });
    expect(reduceLogin(done, { type: 'failed', message: 'Late' })).toBe(done);
  });
});
