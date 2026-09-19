import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { SettingsService } from './settings.service';

describe('SettingsService', () => {
  let service: SettingsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SettingsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
    http.verify();
  });

  it('loads settings before applying the configured theme', async () => {
    const settingsPromise = firstValueFrom(service.load());
    const request = http.expectOne('/api/settings');

    expect(request.request.method).toBe('GET');
    request.flush({ id: 1, theme: 'Dark' });

    await expect(settingsPromise).resolves.toEqual({
      id: 1,
      theme: 'Dark',
    });
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });

  it('falls back to light when settings cannot be loaded', async () => {
    const settingsPromise = firstValueFrom(service.load());
    const request = http.expectOne('/api/settings');

    request.flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });

    await expect(settingsPromise).resolves.toEqual({
      id: 1,
      theme: 'Light',
    });
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(document.documentElement.style.colorScheme).toBe('light');
  });

  it('updates the theme through the settings endpoint', async () => {
    const settingsPromise = firstValueFrom(service.update('Dark'));
    const request = http.expectOne('/api/settings');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ theme: 'Dark' });
    request.flush({ id: 1, theme: 'Dark' });

    await expect(settingsPromise).resolves.toEqual({
      id: 1,
      theme: 'Dark',
    });
    expect(service.theme()).toBe('Dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });
});
