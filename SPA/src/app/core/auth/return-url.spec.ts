import { isLoginUrl, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it.each([
    ['/skills', '/skills'],
    ['/features/1?tab=plans', '/features/1?tab=plans'],
    [null, '/'],
    [undefined, '/'],
    ['', '/'],
    ['https://example.com', '/'],
    ['//example.com', '/'],
    ['/\\example.com', '/'],
    ['skills', '/'],
    ['/login', '/'],
    ['/login?returnUrl=%2Fskills', '/'],
  ])('maps %j to %j', (value, expected) => {
    expect(safeReturnUrl(value)).toBe(expected);
  });
});

describe('isLoginUrl', () => {
  it.each([
    ['/login', true],
    ['/login?returnUrl=%2Fskills', true],
    ['/logins', false],
    ['/skills', false],
  ])('treats %j as login: %j', (url, expected) => {
    expect(isLoginUrl(url)).toBe(expected);
  });
});
