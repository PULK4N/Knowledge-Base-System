import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { ShortIdPipe } from '../../skills/ui/short-id.pipe';
import {
  FeatureSearchRequest,
  FeatureSearchResult,
} from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';

const PAGE_SIZE = 6;

@Component({
  selector: 'app-features-list-page',
  imports: [AsyncPipe, PaginationComponent, RouterLink, ShortIdPipe],
  templateUrl: './features-list.page.html',
  styleUrls: ['./features-list.page.css', '../ui/feature-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturesListPage {
  private readonly features = inject(FeatureService);
  private readonly querySubject = new BehaviorSubject<FeatureSearchRequest>({
    page: 1,
    pageSize: PAGE_SIZE,
    search: '',
  });

  protected readonly state$: Observable<LoadState<FeatureSearchResult>> =
    this.querySubject.pipe(
      debounceTime(200),
      distinctUntilChanged(
        (previous, current) =>
          previous.page === current.page &&
          previous.search.trim() === current.search.trim(),
      ),
      switchMap(request =>
        this.features.search(request).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected search(search: string): void {
    this.querySubject.next({
      ...this.querySubject.value,
      page: 1,
      search,
    });
  }

  protected goToPage(page: number): void {
    this.querySubject.next({ ...this.querySubject.value, page });
  }
}
