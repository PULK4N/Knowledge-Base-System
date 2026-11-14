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
import {
  ListControlOption,
  ListControlsComponent,
} from '../../../shared/list-controls/list-controls.component';
import { ListSortDirection } from '../../../shared/list-state/list-state';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { PolicyProjectDetails } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';
import { MutationRequest, mutationStates } from './mutation-state';
import { ProposalNoteSort, selectProposalNotes } from './proposal-notes';

type NoteMutation = 'add' | 'update' | 'remove';

const NOTE_SORT_OPTIONS: readonly ListControlOption[] = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'title', label: 'Title' },
];

@Component({
  selector: 'app-proposal-notes',
  imports: [AsyncPipe, DatePipe, ListControlsComponent, MarkdownContentComponent],
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
  protected readonly sortOptions = NOTE_SORT_OPTIONS;
  protected readonly sort = signal<ProposalNoteSort>('updatedAt');
  protected readonly direction = signal<ListSortDirection>('Descending');
  protected readonly adding = signal(false);
  protected readonly editingNoteId = signal<string | null>(null);
  protected readonly confirmingNoteId = signal<string | null>(null);
  protected readonly titleError = signal<string | null>(null);
  /** Notes start collapsed; long Markdown notes otherwise push the list off screen. */
  protected readonly expandedNoteIds = signal<ReadonlySet<string>>(new Set());
  protected readonly notes = computed(() =>
    selectProposalNotes(
      this.project().proposalNotes,
      this.search(),
      this.sort(),
      this.direction(),
    ),
  );
  protected readonly mutation$ = mutationStates(this.mutations);

  protected toggleNote(noteId: string): void {
    this.expandedNoteIds.update((expanded) => {
      const next = new Set(expanded);
      if (!next.delete(noteId)) next.add(noteId);
      return next;
    });
  }

  protected expandAll(): void {
    this.expandedNoteIds.set(new Set(this.notes().map((note) => note.noteId)));
  }

  protected collapseAll(): void {
    this.expandedNoteIds.set(new Set());
  }

  protected changeSort(sort: string): void {
    if (sort === 'updatedAt' || sort === 'title') this.sort.set(sort);
  }

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
