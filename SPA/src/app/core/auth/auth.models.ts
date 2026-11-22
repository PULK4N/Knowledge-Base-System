export interface LoginRequest {
  readonly userName: string;
  readonly password: string;
}

export interface LoginResult {
  readonly succeeded: boolean;
  /** True while the administrator still uses the initial password 123. */
  readonly mustChangePassword: boolean;
}

export interface PasswordChange {
  readonly currentPassword: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}
