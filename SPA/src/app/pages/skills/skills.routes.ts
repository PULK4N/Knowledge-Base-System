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
    path: 'new',
    loadComponent: () =>
      import('./feature/skill-create.page').then(
        module => module.SkillCreatePage,
      ),
  },
  {
    path: ':skillId/references/new',
    loadComponent: () =>
      import('./feature/skill-reference-create.page').then(
        module => module.SkillReferenceCreatePage,
      ),
  },
  {
    path: ':skillId/attachments/new',
    loadComponent: () =>
      import('./feature/skill-attachments-add.page').then(
        module => module.SkillAttachmentsAddPage,
      ),
  },
  {
    path: ':skillId/edit',
    loadComponent: () =>
      import('./feature/skill-edit.page').then(
        module => module.SkillEditPage,
      ),
  },
  {
    path: ':skillId/references/edit',
    loadComponent: () =>
      import('./feature/skill-reference-edit.page').then(
        module => module.SkillReferenceEditPage,
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
