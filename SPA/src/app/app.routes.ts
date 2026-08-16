import { Routes } from '@angular/router';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  {
    path: '',
    component: AppShellComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/home/home.page').then(module => module.HomePage),
      },
      {
        path: 'skills',
        loadChildren: () =>
          import('./pages/skills/skills.routes').then(
            module => module.SKILLS_ROUTES,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
