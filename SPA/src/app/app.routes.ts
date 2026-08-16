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
      {
        path: 'policies',
        loadChildren: () =>
          import('./pages/policies/policies.routes').then(
            module => module.GENERAL_POLICIES_ROUTES,
          ),
      },
      {
        path: 'topics',
        loadChildren: () =>
          import('./pages/policies/policies.routes').then(
            module => module.TOPIC_POLICIES_ROUTES,
          ),
      },
      {
        path: 'projects',
        loadChildren: () =>
          import('./pages/policies/policies.routes').then(
            module => module.PROJECT_POLICIES_ROUTES,
          ),
      },
      {
        path: 'memories',
        loadChildren: () =>
          import('./pages/memories/memories.routes').then(
            module => module.MEMORIES_ROUTES,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
