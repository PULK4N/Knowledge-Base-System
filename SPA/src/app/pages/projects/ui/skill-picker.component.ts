import { AsyncPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import {
  Observable,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  startWith,
  switchMap,
} from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';

type SkillResults =
  | { readonly status: 'idle' | 'loading' }
  | { readonly status: 'done'; readonly items: readonly SkillListItem[] }
  | { readonly status: 'error' };

/**
 * Search box that queries the skills API while typing and lets the user pick
 * one skill from the results.
 */
@Component({
  selector: 'app-skill-picker',
  imports: [AsyncPipe],
  templateUrl: './skill-picker.component.html',
  styleUrls: ['../../policies/ui/knowledge-list.css', './skill-picker.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillPickerComponent {
  private readonly skills = inject(SkillService);

  readonly placeholder = input('Search skills');
  /** Skills shown as already added; they can still be picked. */
  readonly takenSkillIds = input<readonly string[]>([]);
  readonly skillPicked = output<SkillListItem | null>();

  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly highlighted = signal(0);
  protected readonly picked = signal<SkillListItem | null>(null);

  protected readonly results$: Observable<SkillResults> = toObservable(this.query).pipe(
    map(query => query.trim()),
    debounceTime(250),
    distinctUntilChanged(),
    switchMap(query =>
      this.skills
        .search({
          page: 1,
          pageSize: 8,
          search: query,
          tag: '',
          hasReferences: null,
          hasAttachments: null,
          sortBy: 'Name',
          sortDirection: 'Ascending',
        })
        .pipe(
          map(result => ({ status: 'done', items: result.items }) as const),
          startWith({ status: 'loading' } as const),
          catchError(() => of({ status: 'error' } as const)),
        ),
    ),
    startWith({ status: 'idle' } as const),
  );

  /** Clears the box after the parent used the picked skill. */
  clear(): void {
    this.query.set('');
    this.choose(null);
  }

  protected type(value: string): void {
    this.query.set(value);
    this.highlighted.set(0);
    this.open.set(true);
    if (this.picked()?.name !== value) this.choose(null);
  }

  protected pick(skill: SkillListItem): void {
    this.query.set(skill.name);
    this.open.set(false);
    this.choose(skill);
  }

  protected keydown(event: KeyboardEvent, items: readonly SkillListItem[]): void {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      this.open.set(true);
      const step = event.key === 'ArrowDown' ? 1 : -1;
      this.highlighted.update(index =>
        items.length === 0 ? 0 : (index + step + items.length) % items.length,
      );
    } else if (event.key === 'Enter' && this.open() && items.length > 0) {
      event.preventDefault();
      this.pick(items[Math.min(this.highlighted(), items.length - 1)]);
    } else if (event.key === 'Escape') {
      this.open.set(false);
    }
  }

  private choose(skill: SkillListItem | null): void {
    this.picked.set(skill);
    this.skillPicked.emit(skill);
  }
}
