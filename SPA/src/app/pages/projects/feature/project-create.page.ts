import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, catchError, exhaustMap, map, of, startWith, tap } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { toUserMessage } from '../../../core/http/load-state';
import { CreateProjectRequest } from '../../policies/data-access/policy.models';
import { PolicyService } from '../../policies/data-access/policy.service';

type CreateState =
  | { readonly status: 'idle' | 'saving' }
  | { readonly status: 'error'; readonly message: string };

@Component({
  selector: 'app-project-create-page',
  imports: [AsyncPipe, FormsModule, RouterLink],
  templateUrl: './project-create.page.html',
  styleUrls: ['./projects-list.page.css', '../ui/project-pages.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectCreatePage {
  private readonly router = inject(Router);
  private readonly projects = inject(PolicyService);
  private readonly createRequests = new Subject<CreateProjectRequest>();

  protected readonly validationError = signal<string | null>(null);

  protected readonly state$ = this.createRequests.pipe(
    exhaustMap(request =>
      this.projects.createProject(request).pipe(
        tap(project => void this.router.navigate(['/projects', project.id])),
        map((): CreateState => ({ status: 'idle' })),
        startWith<CreateState>({ status: 'saving' }),
        catchError(error =>
          of<CreateState>({ status: 'error', message: toUserMessage(error) }),
        ),
      ),
    ),
    startWith<CreateState>({ status: 'idle' }),
  );

  protected create(name: string, overview: string, repositories: string): void {
    const projectName = name.trim();
    if (!projectName) {
      this.validationError.set('A project needs a name.');
      return;
    }

    this.validationError.set(null);
    this.createRequests.next({
      projectName,
      projectDescription: overview.trim(),
      repositoryPaths: repositories
        .split('\n')
        .map(path => path.trim())
        .filter(path => path.length > 0),
    });
  }
}
