import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { ListControlsComponent } from '../../../shared/list-controls/list-controls.component';
import { ListRowComponent } from '../../../shared/list-row/list-row.component';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { PolicyProjectSearchResult } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { policySearchRequests } from '../../policies/feature/policy-search-requests';

@Component({
  selector: 'app-projects-list-page',
  imports: [
    AsyncPipe,
    ListControlsComponent,
    ListRowComponent,
    PaginationComponent,
    RouterLink,
  ],
  templateUrl: './projects-list.page.html',
  styleUrls: [
    '../../policies/feature/policy-directory-list.page.css',
    '../../policies/ui/knowledge-list.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsListPage {
  private readonly projects = inject(PolicyService);
  private readonly searchRequests = new Subject<string>();
  private readonly pageRequests = new Subject<number>();

  protected readonly state$: Observable<LoadState<PolicyProjectSearchResult>> =
    policySearchRequests(this.searchRequests, this.pageRequests).pipe(
      switchMap(request =>
        this.projects.searchProjects(request).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected search(search: string): void {
    this.searchRequests.next(search);
  }

  protected goToPage(page: number): void {
    this.pageRequests.next(page);
  }
}
