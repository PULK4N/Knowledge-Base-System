import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, catchError, combineLatest, map, of, startWith } from 'rxjs';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';

type OverviewMutation = 'overview' | 'repository' | 'topic' | 'remove-topic';

@Component({
  selector: 'app-project-overview',
  imports: [AsyncPipe, MarkdownContentComponent, RouterLink],
  templateUrl: './project-overview.component.html',
  styleUrls: ['./project-pages.css', '../../policies/feature/policy-list.page.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectOverviewComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<OverviewMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly emptyBlocks = [];
  protected readonly editingOverview = signal(false);
  protected readonly confirmingTopic = signal<string | null>(null);
  protected readonly vm$ = combineLatest({
    mutation: mutationStates(this.mutations),
    topicNames: this.projects
      .searchTopics({ page: 1, pageSize: 100, search: '' })
      .pipe(
        map(result => result.items.map(topic => topic.name)),
        catchError(() => of([] as string[])),
        startWith([] as string[]),
      ),
  });

  /** Topics created under Policies that the project is not connected to yet. */
  protected availableTopics(
    allTopics: readonly string[],
    project: PolicyProjectDetails,
  ): readonly string[] {
    return allTopics.filter(topic => !project.topicNames.includes(topic));
  }

  protected saveOverview(project: PolicyProjectDetails, overview: string): void {
    this.mutations.next({
      kind: 'overview',
      request: this.projects.updateProject(project.id, project.name, overview.trim()),
      done: () => this.editingOverview.set(false),
    });
  }

  protected addRepository(projectId: string, repositoryPath: string): void {
    const value = repositoryPath.trim();
    if (!value) return;

    this.mutations.next({
      kind: 'repository',
      request: this.projects.addProjectRepository(projectId, value),
    });
  }

  protected addTopic(projectId: string, topicName: string): void {
    const value = topicName.trim();
    if (!value) return;

    this.mutations.next({
      kind: 'topic',
      request: this.projects.addProjectTopic(projectId, value),
    });
  }

  protected removeTopic(projectId: string, topicName: string): void {
    this.mutations.next({
      kind: 'remove-topic',
      request: this.projects.removeProjectTopic(projectId, topicName),
      done: () => this.confirmingTopic.set(null),
    });
  }
}
