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
    ],
  },
];
