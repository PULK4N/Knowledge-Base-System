import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { Subject } from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { pageOf } from './page-of';
import { SkillPickerComponent } from './skill-picker.component';

type SkillMutation = 'add' | 'remove';

@Component({
  selector: 'app-implicit-skills',
  imports: [AsyncPipe, PaginationComponent, RouterLink, SkillPickerComponent],
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

  protected readonly page = signal(1);
  protected readonly skills = computed(() =>
    pageOf(this.project().skills, this.page(), 10),
  );
  protected readonly confirmingSkillId = signal<string | null>(null);
  protected readonly nameError = signal<string | null>(null);
  protected readonly pickedSkill = signal<SkillListItem | null>(null);
  protected readonly linkedSkillIds = computed(() =>
    this.project().skills.map(skill => skill.skillId),
  );
  protected readonly mutation$ = mutationStates(this.mutations);

  protected addSkill(projectId: string, picker: SkillPickerComponent): void {
    const skill = this.pickedSkill();
    if (!skill) {
      this.nameError.set('Pick a skill from the search results.');
      return;
    }

    this.nameError.set(null);
    this.mutations.next({
      kind: 'add',
      request: this.projects.addProjectSkill(projectId, skill.id),
      done: () => picker.clear(),
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
