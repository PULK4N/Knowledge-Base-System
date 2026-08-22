import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  exhaustMap,
  map,
  of,
  shareReplay,
  startWith,
  tap,
} from 'rxjs';
import { toUserMessage } from '../../../core/http/load-state';
import { AddSkillRequest } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';

type CreateState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-skill-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './skill-create.page.html',
  styleUrl: '../ui/editor-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillCreatePage {
  private readonly router = inject(Router);
  private readonly skills = inject(SkillService);
  private readonly createRequests = new Subject<AddSkillRequest>();

  protected readonly state$: Observable<CreateState> = this.createRequests.pipe(
    exhaustMap(request =>
      this.skills.create(request).pipe(
        tap(skill => void this.router.navigate(['/skills', skill.id])),
        map(() => ({ status: 'idle' }) as const),
        startWith({ status: 'saving' } as const),
        catchError(error =>
          of({ status: 'error', message: toUserMessage(error) } as const),
        ),
      ),
    ),
    startWith({ status: 'idle' } as const),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected create(
    name: string,
    description: string,
    content: string,
    tags: string,
  ): void {
    this.createRequests.next({
      name: name.trim(),
      description,
      content,
      tags: [...new Set(tags.split(',').map(tag => tag.trim()).filter(Boolean))],
    });
  }
}
