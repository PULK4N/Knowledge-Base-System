import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  exhaustMap,
  filter,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { Skill } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';

interface AttachmentAction {
  readonly skillId: string;
  readonly files: readonly File[];
}

type UploadState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-attachments-add-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './skill-attachments-add.page.html',
  styleUrl: '../ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillAttachmentsAddPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);
  private readonly uploadRequests = new Subject<AttachmentAction>();

  private readonly skillState$: Observable<LoadState<Skill>> =
    this.route.paramMap.pipe(
      map(params => params.get('skillId')),
      filter((skillId): skillId is string => skillId !== null),
      distinctUntilChanged(),
      switchMap(skillId =>
        this.skills.watch(skillId).pipe(
          map(data => ({ status: 'success', data }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
    );

  private readonly mutation$: Observable<UploadState> =
    this.uploadRequests.pipe(
      exhaustMap(({ skillId, files }) =>
        this.skills.addAttachments(skillId, files).pipe(
          tap(() =>
            void this.router.navigate(['/skills', skillId], {
              queryParams: { tab: 'attachments' },
            }),
          ),
          map(() => ({ status: 'idle' }) as const),
          startWith({ status: 'saving' } as const),
          catchError(error =>
            of({ status: 'error', message: toUserMessage(error) } as const),
          ),
        ),
      ),
      startWith({ status: 'idle' } as const),
    );

  protected readonly vm$ = combineLatest({
    state: this.skillState$,
    mutation: this.mutation$,
  }).pipe(shareReplay({ bufferSize: 1, refCount: true }));

  protected upload(skillId: string, files: FileList | null): void {
    const selectedFiles = files ? Array.from(files) : [];
    if (selectedFiles.length === 0) return;

    this.uploadRequests.next({ skillId, files: selectedFiles });
  }
}
