const loginPath = '/login';

/**
 * The page to open after login. Only paths inside this app are accepted, so a crafted link cannot
 * send the user to another site after they log in.
 */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return '/';
  return isLoginUrl(value) ? '/' : value;
}

export function isLoginUrl(url: string): boolean {
  return url === loginPath || url.startsWith(`${loginPath}?`) || url.startsWith(`${loginPath}/`);
}
