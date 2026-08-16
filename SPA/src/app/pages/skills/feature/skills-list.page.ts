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
import {
  SkillSearchRequest,
  SkillSearchResult,
} from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';
import { visiblePages } from '../../../shared/pagination/visible-pages';
import { ShortIdPipe } from '../ui/short-id.pipe';

const PAGE_SIZE = 5;

interface SkillsListView {
  readonly result: SkillSearchResult;
  readonly pages: readonly number[];
  readonly firstVisibleItem: number;
  readonly lastVisibleItem: number;
}

@Component({
  selector: 'app-skills-list-page',
  imports: [AsyncPipe, RouterLink, ShortIdPipe],
  templateUrl: './skills-list.page.html',
  styleUrl: './skills-list.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillsListPage {
  private readonly skills = inject(SkillService);
  private readonly querySubject = new BehaviorSubject<SkillSearchRequest>({
    page: 1,
    pageSize: PAGE_SIZE,
    search: '',
  });

  protected readonly state$: Observable<LoadState<SkillsListView>> =
    this.querySubject.pipe(
      debounceTime(200),
      distinctUntilChanged(
        (previous, current) =>
          previous.page === current.page &&
          previous.search.trim() === current.search.trim(),
      ),
      switchMap(request =>
        this.skills.search(request).pipe(
          map(result => ({
            status: 'success',
            data: {
              result,
              pages: visiblePages(result.totalPages, result.page),
              firstVisibleItem:
                (result.page - 1) * result.pageSize + 1,
              lastVisibleItem: Math.min(
                result.page * result.pageSize,
                result.totalCount,
              ),
            },
          }) as const),
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
    const current = this.querySubject.value;
    if (page < 1 || page === current.page) return;

    this.querySubject.next({ ...current, page });
  }
}
