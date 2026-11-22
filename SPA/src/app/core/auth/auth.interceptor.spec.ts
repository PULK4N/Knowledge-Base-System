import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let navigate: ReturnType<typeof vi.fn>;
  let currentUrl: string;

  beforeEach(() => {
    currentUrl = '/skills';
    navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: Router,
          useValue: {
            navigate,
            get url() {
              return currentUrl;
            },
          },
        },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  async function fail(url: string, status: number): Promise<unknown> {
    const result = firstValueFrom(http.get(url)).catch((error: unknown) => error);
    controller.expectOne(url).flush(null, { status, statusText: 'Error' });
    return result;
  }

  it('sends a 401 to the login page with the current page as return URL', async () => {
    const error = await fail('/api/skills', 401);

    expect(error).toMatchObject({ status: 401 });
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/skills' } });
  });

  it.each([
    ['/api/auth/login', 401, '/login'],
    ['/api/skills', 401, '/login?returnUrl=%2Fskills'],
    ['/api/skills', 403, '/skills'],
    ['/api/skills', 500, '/skills'],
  ])('does not navigate for %s answering %s on %s', async (url, status, page) => {
    currentUrl = page;

    const error = await fail(url, status);

    expect(error).toMatchObject({ status });
    expect(navigate).not.toHaveBeenCalled();
  });
});
