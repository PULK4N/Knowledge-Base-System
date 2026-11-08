import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, combineLatest } from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { findSkillByName, loadSkillOptions } from './skill-options';

type SkillMutation = 'add' | 'remove';

@Component({
  selector: 'app-implicit-skills',
  imports: [AsyncPipe, RouterLink],
  templateUrl: './implicit-skills.component.html',
  styleUrls: [
    '../../policies/feature/policy-directory-list.page.css',
    '../../policies/ui/knowledge-list.css',
    './project-pages.css',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImplicitSkillsComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<SkillMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly confirmingSkillId = signal<string | null>(null);
  protected readonly nameError = signal<string | null>(null);
  protected readonly vm$ = combineLatest({
    mutation: mutationStates(this.mutations),
    skillOptions: loadSkillOptions(inject(SkillService)),
  });

  protected addSkill(
    projectId: string,
    name: string,
    options: readonly SkillListItem[],
  ): void {
    if (!name.trim()) return;

    const skill = findSkillByName(options, name);
    if (!skill) {
      this.nameError.set(`No skill is named "${name.trim()}".`);
      return;
    }

    this.nameError.set(null);
    this.mutations.next({
      kind: 'add',
      request: this.projects.addProjectSkill(projectId, skill.id),
    });
  }

  protected removeSkill(projectId: string, skillId: string): void {
    this.mutations.next({
      kind: 'remove',
      request: this.projects.removeProjectSkill(projectId, skillId),
      done: () => this.confirmingSkillId.set(null),
    });
  }
}
