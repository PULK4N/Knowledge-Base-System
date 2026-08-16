import { Routes } from '@angular/router';

export const SKILLS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/skills-list.page').then(
        module => module.SkillsListPage,
      ),
  },
  {
    path: ':skillId/references',
    loadComponent: () =>
      import('./feature/skill-reference.page').then(
        module => module.SkillReferencePage,
      ),
  },
  {
    path: ':skillId',
    loadComponent: () =>
      import('./feature/skill-details.page').then(
        module => module.SkillDetailsPage,
      ),
  },
];
