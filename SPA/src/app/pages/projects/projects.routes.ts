import { Routes } from '@angular/router';

export const PROJECTS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/projects-list.page').then(
        module => module.ProjectsListPage,
      ),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./feature/project-create.page').then(
        module => module.ProjectCreatePage,
      ),
  },
  {
    path: ':projectId',
    loadComponent: () =>
      import('./feature/project-details.page').then(
        module => module.ProjectDetailsPage,
      ),
  },
];
