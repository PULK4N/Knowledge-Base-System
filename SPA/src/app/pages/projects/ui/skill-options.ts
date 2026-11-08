import { Observable, catchError, map, of, shareReplay, startWith } from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';

/** Skills offered when adding a skill by name; the API allows 100 per page. */
export function loadSkillOptions(
  skills: SkillService,
): Observable<readonly SkillListItem[]> {
  return skills
    .search({
      page: 1,
      pageSize: 100,
      search: '',
      tag: '',
      hasReferences: null,
      hasAttachments: null,
      sortBy: 'Name',
      sortDirection: 'Ascending',
    })
    .pipe(
      map(result => result.items),
      catchError(() => of([])),
      startWith([]),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
}

export function findSkillByName(
  options: readonly SkillListItem[],
  name: string,
): SkillListItem | undefined {
  const normalized = name.trim().toLowerCase();
  return options.find(option => option.name.toLowerCase() === normalized);
}
