import { AsyncPipe } from '@angular/common';
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
} from 'rxjs';
import { SettingsService } from '../core/settings/settings.service';

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
  private readonly themeToggleRequests = new Subject<void>();

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

  protected toggleTheme(): void {
    this.themeToggleRequests.next();
  }
}
