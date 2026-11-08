import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  combineLatest,
  distinctUntilChanged,
  filter,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { PolicyCollectionComponent } from '../../policies/ui/policy-collection.component';
import { ImplicitSkillsComponent } from '../ui/implicit-skills.component';
import { ProjectFeaturesComponent } from '../ui/project-features.component';
import { ProjectOverviewComponent } from '../ui/project-overview.component';
import { ProjectReviewersComponent } from '../ui/project-reviewers.component';
import { ProposalNotesComponent } from '../ui/proposal-notes.component';
import { PROJECT_TABS, parseProjectTab } from './project-tabs';

@Component({
  selector: 'app-project-details-page',
  imports: [
    AsyncPipe,
    ImplicitSkillsComponent,
    PolicyCollectionComponent,
    ProjectFeaturesComponent,
    ProjectOverviewComponent,
    ProjectReviewersComponent,
    ProposalNotesComponent,
    RouterLink,
  ],
  templateUrl: './project-details.page.html',
  styleUrls: ['./project-details.page.css', './projects-list.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailsPage {
  private readonly route = inject(ActivatedRoute);
  private readonly projects = inject(PolicyService);

  protected readonly tabs = PROJECT_TABS;

  private readonly state$: Observable<LoadState<PolicyProjectDetails>> =
    this.route.paramMap.pipe(
      map(params => params.get('projectId')),
      filter((projectId): projectId is string => projectId !== null),
      distinctUntilChanged(),
      switchMap(projectId =>
        this.projects.watchProject(projectId).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected readonly vm$ = combineLatest({
    state: this.state$,
    activeTab: this.route.queryParamMap.pipe(
      map(params => parseProjectTab(params.get('tab'))),
      distinctUntilChanged(),
    ),
  });
}
