import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, shareReplay, tap } from 'rxjs';
import { Settings, Theme } from './settings.models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly document = inject(DOCUMENT);
  private readonly http = inject(HttpClient);
  private readonly currentTheme = signal<Theme>('Light');

  readonly theme = this.currentTheme.asReadonly();

  private readonly settings$ = this.http.get<Settings>('/api/settings').pipe(
    tap(settings => this.applyTheme(settings.theme)),
    catchError(error => {
      console.error('Could not load application settings.', error);
      const fallback: Settings = { id: 1, theme: 'Light' };
      this.applyTheme(fallback.theme);
      return of(fallback);
    }),
    shareReplay({ bufferSize: 1, refCount: false }),
  );

  load(): Observable<Settings> {
    return this.settings$;
  }

  update(theme: Theme): Observable<Settings> {
    return this.http
      .put<Settings>('/api/settings', { theme })
      .pipe(tap(settings => this.applyTheme(settings.theme)));
  }

  toggle(): Observable<Settings> {
    return this.update(
      this.currentTheme() === 'Dark' ? 'Light' : 'Dark',
    );
  }

  private applyTheme(theme: Theme): void {
    const normalizedTheme = theme.toLowerCase();
    const root = this.document.documentElement;

    root.dataset['theme'] = normalizedTheme;
    root.style.colorScheme = normalizedTheme;
    this.currentTheme.set(theme);
  }
}
