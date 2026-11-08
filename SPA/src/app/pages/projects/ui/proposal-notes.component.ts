import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { Subject } from 'rxjs';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { ProposalNoteSort, selectProposalNotes } from './proposal-notes';

type NoteMutation = 'add' | 'update' | 'remove';

@Component({
  selector: 'app-proposal-notes',
  imports: [AsyncPipe, DatePipe, MarkdownContentComponent],
  templateUrl: './proposal-notes.component.html',
  styleUrl: './project-pages.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProposalNotesComponent {
  private readonly projects = inject(PolicyService);
  private readonly mutations = new Subject<MutationRequest<NoteMutation>>();

  readonly project = input.required<PolicyProjectDetails>();

  protected readonly emptyBlocks = [];
  protected readonly search = signal('');
  protected readonly sort = signal<ProposalNoteSort>('updatedAt');
  protected readonly adding = signal(false);
  protected readonly editingNoteId = signal<string | null>(null);
  protected readonly confirmingNoteId = signal<string | null>(null);
  protected readonly titleError = signal<string | null>(null);
  protected readonly notes = computed(() =>
    selectProposalNotes(this.project().proposalNotes, this.search(), this.sort()),
  );
  protected readonly mutation$ = mutationStates(this.mutations);

  protected addNote(projectId: string, title: string, content: string): void {
    if (!this.checkTitle(title)) return;

    this.mutations.next({
      kind: 'add',
      request: this.projects.addProposalNote(projectId, { title: title.trim(), content }),
      done: () => this.adding.set(false),
    });
  }

  protected updateNote(
    projectId: string,
    noteId: string,
    title: string,
    content: string,
  ): void {
    if (!this.checkTitle(title)) return;

    this.mutations.next({
      kind: 'update',
      request: this.projects.updateProposalNote(projectId, noteId, {
        title: title.trim(),
        content,
      }),
      done: () => this.editingNoteId.set(null),
    });
  }

  protected removeNote(projectId: string, noteId: string): void {
    this.mutations.next({
      kind: 'remove',
      request: this.projects.removeProposalNote(projectId, noteId),
      done: () => this.confirmingNoteId.set(null),
    });
  }

  private checkTitle(title: string): boolean {
    const valid = title.trim().length > 0;
    this.titleError.set(valid ? null : 'A proposal note needs a title.');
    return valid;
  }
}
