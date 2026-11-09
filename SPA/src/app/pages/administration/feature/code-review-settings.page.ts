import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  Subject, catchError, combineLatest, exhaustMap, map, of, shareReplay, startWith, switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { CodeReviewSettings } from '../data-access/code-review-settings.models';
import { CodeReviewSettingsService } from '../data-access/code-review-settings.service';
import { CodeReviewSettingsFormComponent } from '../ui/code-review-settings-form.component';

type SaveState =
  | { readonly status: 'idle' | 'saving' | 'saved' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-code-review-settings-page',
  imports: [AsyncPipe, CodeReviewSettingsFormComponent],
  templateUrl: './code-review-settings.page.html',
  styleUrl: './code-review-settings.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CodeReviewSettingsPage {
  private readonly settings = inject(CodeReviewSettingsService);
  private readonly reloadRequests = new Subject<void>();
  private readonly saveRequests = new Subject<CodeReviewSettings>();

  private readonly load$ = this.reloadRequests.pipe(
    startWith(undefined),
    switchMap(() => this.settings.watch().pipe(
      map(data => ({ status: 'success', data }) as LoadState<CodeReviewSettings>),
      startWith({ status: 'loading' } as LoadState<CodeReviewSettings>),
      catchError(error => of({ status: 'error', message: toUserMessage(error) } as LoadState<CodeReviewSettings>)),
    )),
  );

  private readonly save$ = this.saveRequests.pipe(
    exhaustMap(value => this.settings.save(value).pipe(
      map(() => ({ status: 'saved' }) as SaveState),
      startWith({ status: 'saving' } as SaveState),
      catchError(() => of({ status: 'error', message: 'Could not save reviewer settings. Please try again.' } as SaveState)),
    )),
    startWith({ status: 'idle' } as SaveState),
  );

  protected readonly vm$ = combineLatest({ load: this.load$, save: this.save$ }).pipe(
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected reload(): void { this.reloadRequests.next(); }
  protected save(value: CodeReviewSettings): void { this.saveRequests.next(value); }
}
