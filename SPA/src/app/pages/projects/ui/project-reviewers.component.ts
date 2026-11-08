import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, combineLatest } from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import {
  PolicyProjectDetails,
  ProjectSkill,
  REVIEW_LEVELS,
  ReviewLevel,
} from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { toReviewLevelModels } from './project-review';
import { findSkillByName, loadSkillOptions } from './skill-options';

type ReviewerMutation = 'add' | 'update' | 'remove' | 'models';

@Component({
  selector: 'app-project-reviewers',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './project-reviewers.component.html',
  styleUrl: './project-pages.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectReviewersComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<ReviewerMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly reviewLevels = REVIEW_LEVELS;
  protected readonly reviewers = computed(() =>
    this.project().skills.filter(skill => skill.usedForReview),
  );
  protected readonly editingSkillId = signal<string | null>(null);
  protected readonly editingModels = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly vm$ = combineLatest({
    mutation: mutationStates(this.mutations),
    skillOptions: loadSkillOptions(inject(SkillService)),
  });

  /** Links the skill when needed and switches review on with the level and notes. */
  protected addReviewer(
    project: PolicyProjectDetails,
    name: string,
    level: ReviewLevel | '',
    notes: string,
    options: readonly SkillListItem[],
  ): void {
    const skill = findSkillByName(options, name);
    if (!skill) {
      this.formError.set(name.trim() ? `No skill is named "${name.trim()}".` : 'Choose a skill.');
      return;
    }
    if (!level) {
      this.formError.set('Choose a review level.');
      return;
    }

    this.formError.set(null);
    const review = { usedForReview: true, reviewLevel: level, reviewNotes: notes.trim() };
    const linked = project.skills.some(linkedSkill => linkedSkill.skillId === skill.id);
    this.mutations.next({
      kind: 'add',
      request: linked
        ? this.projects.updateProjectSkill(project.id, skill.id, review)
        : this.projects.addProjectSkill(project.id, skill.id, review),
    });
  }

  protected updateReviewer(
    projectId: string,
    reviewer: ProjectSkill,
    level: ReviewLevel,
    notes: string,
  ): void {
    this.mutations.next({
      kind: 'update',
      request: this.projects.updateProjectSkill(projectId, reviewer.skillId, {
        usedForReview: true,
        reviewLevel: level,
        reviewNotes: notes.trim(),
      }),
      done: () => this.editingSkillId.set(null),
    });
  }

  /** Switches review off; the skill stays an implicit skill. */
  protected removeReviewer(projectId: string, reviewer: ProjectSkill): void {
    this.mutations.next({
      kind: 'remove',
      request: this.projects.updateProjectSkill(projectId, reviewer.skillId, {
        usedForReview: false,
        reviewLevel: reviewer.reviewLevel,
        reviewNotes: reviewer.reviewNotes,
      }),
    });
  }

  protected saveModels(
    projectId: string,
    form: Readonly<Record<string, string | undefined>>,
  ): void {
    const result = toReviewLevelModels(form);
    if (result.status === 'invalid') {
      this.formError.set(result.message);
      return;
    }

    this.formError.set(null);
    this.mutations.next({
      kind: 'models',
      request: this.projects.setProjectReviewLevelModels(projectId, result.levelModels),
      done: () => this.editingModels.set(false),
    });
  }
}
