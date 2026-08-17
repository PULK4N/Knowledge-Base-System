import { Routes } from '@angular/router';

export const ADMINISTRATION_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/projection-administration.page').then(
        module => module.ProjectionAdministrationPage,
      ),
  },
];
