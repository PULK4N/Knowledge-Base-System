import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  map,
  of,
  shareReplay,
  startWith,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../core/http/load-state';
import { OverviewCounts, OverviewService } from './overview.service';

@Component({
  selector: 'app-home-page',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './home.page.html',
  styleUrl: './home.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly overview = inject(OverviewService);

  protected readonly state$: Observable<LoadState<OverviewCounts>> =
    this.overview.getCounts().pipe(
      map(data => ({ status: 'success', data }) as const),
      startWith({ status: 'loading' } as const),
      catchError(error =>
        of({ status: 'error', message: toUserMessage(error) } as const),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
}
