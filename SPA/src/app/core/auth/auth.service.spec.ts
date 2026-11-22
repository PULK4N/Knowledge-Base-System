import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the credentials and returns the login result', async () => {
    const resultPromise = firstValueFrom(
      service.login({ userName: 'admin', password: '123' }),
    );
    const request = http.expectOne('/api/auth/login');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ userName: 'admin', password: '123' });
    request.flush({ succeeded: true, mustChangePassword: true });

    await expect(resultPromise).resolves.toEqual({
      succeeded: true,
      mustChangePassword: true,
    });
  });

  it('returns a failed result for rejected credentials', async () => {
    const resultPromise = firstValueFrom(
      service.login({ userName: 'admin', password: 'wrong' }),
    );

    http
      .expectOne('/api/auth/login')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(resultPromise).resolves.toEqual({
      succeeded: false,
      mustChangePassword: false,
    });
  });

  it('keeps other login errors as errors', async () => {
    const resultPromise = firstValueFrom(
      service.login({ userName: 'admin', password: '123' }),
    );

    http
      .expectOne('/api/auth/login')
      .flush(null, { status: 500, statusText: 'Server Error' });

    await expect(resultPromise).rejects.toMatchObject({ status: 500 });
  });

  it('posts a password change', async () => {
    const change = {
      currentPassword: 'old',
      newPassword: 'new',
      confirmPassword: 'new',
    };
    const resultPromise = firstValueFrom(service.changePassword(change));
    const request = http.expectOne('/api/auth/password');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(change);
    request.flush(null, { status: 204, statusText: 'No Content' });

    await expect(resultPromise).resolves.toBeUndefined();
  });

  it('posts a logout', async () => {
    const resultPromise = firstValueFrom(service.logout());
    const request = http.expectOne('/api/auth/logout');

    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });

    await expect(resultPromise).resolves.toBeUndefined();
  });
});
