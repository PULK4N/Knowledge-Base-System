export interface LoginRequest {
  readonly userName: string;
  readonly password: string;
}

export interface LoginResult {
  readonly succeeded: boolean;
  /** True while the initial admin / 123 credentials are still in use. */
  readonly mustChangePassword: boolean;
}

export interface InitialCredentials {
  readonly currentPassword: string;
  readonly newUserName: string | null;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

export interface PasswordChange {
  readonly currentPassword: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}
