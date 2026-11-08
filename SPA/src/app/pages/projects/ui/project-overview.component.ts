import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';

type OverviewMutation = 'overview' | 'repository' | 'topic' | 'remove-topic';

@Component({
  selector: 'app-project-overview',
  imports: [AsyncPipe, MarkdownContentComponent, RouterLink],
  templateUrl: './project-overview.component.html',
  styleUrl: './project-pages.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectOverviewComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<OverviewMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly emptyBlocks = [];
  protected readonly editingOverview = signal(false);
  protected readonly confirmingTopic = signal<string | null>(null);
  protected readonly mutation$ = mutationStates(this.mutations);

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
