import { AsyncPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, catchError, map, of, startWith, switchMap } from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import {
  Feature,
  PlaywrightScript,
  PlaywrightScriptSnapshot,
  Specification,
  SpecificationAction,
} from '../data-access/feature.models';
import { FeatureLinkPickerComponent } from './feature-link-picker.component';
import { OriginLinkComponent } from './origin-link.component';
import { OriginLinkPipe } from './origin-link.pipe';
import { ScrollIntoViewDirective } from './scroll-into-view.directive';
import { itemAnchor } from './specification-origin';
import { definitionOfDoneOptions, labelsOf } from './specification-labels';

type ScriptEditor =
  | { readonly mode: 'edit'; readonly id: string }
  | { readonly mode: 'snapshot'; readonly id: string };

/**
 * Shows Playwright script paths with the definitions of done they confirm and
 * their snapshots, and emits add, update, remove and snapshot changes.
 */
@Component({
  selector: 'app-specification-scripts',
  imports: [
    AsyncPipe,
    DatePipe,
    FeatureLinkPickerComponent,
    FormsModule,
    OriginLinkComponent,
    OriginLinkPipe,
    ScrollIntoViewDirective,
  ],
  templateUrl: './specification-scripts.component.html',
  styleUrl: './feature-specification.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpecificationScriptsComponent {
  readonly specification = input.required<Specification>();
  /** Set on the feature screen so copies show their origin link. */
  readonly feature = input<Feature | null>(null);
  readonly saving = input(false);
  readonly fragment = input<string | null>(null);
  /** Reads a script's snapshot code, which the specification leaves out. */
  readonly loadSnapshot = input.required<(scriptId: string) => Observable<PlaywrightScriptSnapshot>>();

  readonly action = output<SpecificationAction>();

  protected readonly anchor = itemAnchor;
  protected readonly adding = signal(false);
  protected readonly editor = signal<ScriptEditor | null>(null);
  protected readonly confirmingRemovalId = signal<string | null>(null);
  protected readonly pickedDefinitionOfDoneIds = signal<readonly string[]>([]);
  /** The script whose snapshot code is shown. */
  protected readonly openSnapshotId = signal<string | null>(null);

  protected readonly definitionOfDoneOptions = computed(() =>
    definitionOfDoneOptions(this.specification()),
  );

  /** The open snapshot, loaded again whenever another script is opened. */
  protected readonly snapshot$: Observable<LoadState<PlaywrightScriptSnapshot> | null> =
    toObservable(this.openSnapshotId).pipe(
      switchMap(scriptId =>
        scriptId === null
          ? of(null)
          : this.loadSnapshot()(scriptId).pipe(
              map(snapshot => ({ status: 'success', data: snapshot }) as const),
              startWith({ status: 'loading' } as const),
              catchError(error => of({ status: 'error', message: toUserMessage(error) } as const)),
            ),
      ),
    );

  protected definitionOfDoneLabels(script: PlaywrightScript): readonly string[] {
    return labelsOf(script.definitionOfDoneIds, this.specification().definitionsOfDone);
  }

  protected isEditing(mode: ScriptEditor['mode'], id: string): boolean {
    const editor = this.editor();
    return editor?.mode === mode && editor.id === id;
  }

  protected startAdding(): void {
    this.pickedDefinitionOfDoneIds.set([]);
    this.adding.set(!this.adding());
  }

  protected startEditing(script: PlaywrightScript): void {
    this.pickedDefinitionOfDoneIds.set(script.definitionOfDoneIds);
    this.editor.set({ mode: 'edit', id: script.id });
  }

  protected toggleSnapshot(scriptId: string): void {
    this.openSnapshotId.set(this.openSnapshotId() === scriptId ? null : scriptId);
  }

  protected add(path: string, snapshotContent: string): void {
    this.action.emit({
      kind: 'add-scripts',
      scripts: [{
        path: path.trim(),
        definitionOfDoneIds: this.pickedDefinitionOfDoneIds(),
        snapshotContent: snapshotContent.trim() ? snapshotContent : null,
      }],
    });
    this.adding.set(false);
  }

  protected update(id: string, path: string): void {
    this.action.emit({
      kind: 'update-scripts',
      scripts: [{ id, path: path.trim(), definitionOfDoneIds: this.pickedDefinitionOfDoneIds() }],
    });
    this.editor.set(null);
  }

  /** Saves new snapshot code; empty code removes the snapshot. */
  protected saveSnapshot(id: string, content: string): void {
    this.action.emit({
      kind: 'set-snapshots',
      snapshots: [{ id, content: content.trim() ? content : null }],
    });
    this.editor.set(null);
    this.openSnapshotId.set(null);
  }

  protected remove(id: string): void {
    this.action.emit({ kind: 'remove-scripts', scriptIds: [id] });
    this.confirmingRemovalId.set(null);
  }
}
