import { AsyncPipe, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  tap,
} from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { SettingsService } from '../core/settings/settings.service';

type LogoutState =
  | { readonly status: 'idle' }
  | { readonly status: 'logging-out' }
  | { readonly status: 'error' };

type ThemeToggleState =
  | { readonly status: 'idle' }
  | { readonly status: 'updating' }
  | { readonly status: 'error' };

@Component({
  selector: 'app-shell',
  imports: [AsyncPipe, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  private readonly settings = inject(SettingsService);
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly themeToggleRequests = new Subject<void>();
  private readonly logoutRequests = new Subject<void>();

  protected readonly currentTheme = this.settings.theme;
  protected readonly themeToggleLabel = computed(() =>
    this.currentTheme() === 'Dark'
      ? 'Switch to light mode'
      : 'Switch to dark mode',
  );
  protected readonly themeToggleState$: Observable<ThemeToggleState> =
    this.themeToggleRequests.pipe(
      exhaustMap(() =>
        this.settings.toggle().pipe(
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'updating' } as const),
          catchError(() => of({ status: 'error' } as const)),
        ),
      ),
      startWith({ status: 'idle' } as const),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  /** A full page load after logout drops every cached entity of the logged-out session. */
  protected readonly logoutState$: Observable<LogoutState> =
    this.logoutRequests.pipe(
      exhaustMap(() =>
        this.auth.logout().pipe(
          tap(() => this.document.location.assign('/login')),
          map(() => ({ status: 'logging-out' }) as const),
          startWith({ status: 'logging-out' } as const),
          catchError(() => of({ status: 'error' } as const)),
        ),
      ),
      startWith({ status: 'idle' } as const),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected toggleTheme(): void {
    this.themeToggleRequests.next();
  }

  protected logout(): void {
    this.logoutRequests.next();
  }
}
