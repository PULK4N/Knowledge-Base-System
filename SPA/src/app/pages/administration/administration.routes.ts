import { Routes } from '@angular/router';

export const ADMINISTRATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./feature/administration.page').then(
        module => module.AdministrationPage,
      ),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'projections',
      },
      {
        path: 'projections',
        loadComponent: () =>
          import('./feature/projection-administration.page').then(
            module => module.ProjectionAdministrationPage,
          ),
      },
      {
        path: 'outbox',
        loadComponent: () =>
          import('./feature/outbox-administration.page').then(
            module => module.OutboxAdministrationPage,
          ),
      },
      {
        path: 'database',
        loadComponent: () =>
          import('./feature/database-administration.page').then(
            module => module.DatabaseAdministrationPage,
          ),
      },
      {
        path: 'queues',
        loadComponent: () =>
          import('./feature/queue-administration.page').then(
            module => module.QueueAdministrationPage,
          ),
      },
      {
        path: 'projection-runner',
        loadComponent: () =>
          import('./feature/projection-runner.page').then(
            module => module.ProjectionRunnerPage,
          ),
      },
    ],
  },
];
