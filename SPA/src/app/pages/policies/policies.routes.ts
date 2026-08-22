import { Routes } from '@angular/router';

export const GENERAL_POLICIES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    data: { policyScope: 'general' },
    loadComponent: () =>
      import('./feature/policy-list.page').then(
        module => module.PolicyListPage,
      ),
  },
  {
    path: 'new',
    data: { policyScope: 'general' },
    loadComponent: () =>
      import('./feature/policy-create.page').then(
        module => module.PolicyCreatePage,
      ),
  },
];

export const TOPIC_POLICIES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    data: { directoryKind: 'topics' },
    loadComponent: () =>
      import('./feature/policy-directory-list.page').then(
        module => module.PolicyDirectoryListPage,
      ),
  },
  {
    path: 'new',
    data: { directoryKind: 'topics' },
    loadComponent: () =>
      import('./feature/policy-directory-create.page').then(
        module => module.PolicyDirectoryCreatePage,
      ),
  },
  {
    path: ':topicName/policies/new',
    data: { policyScope: 'topic' },
    loadComponent: () =>
      import('./feature/policy-create.page').then(
        module => module.PolicyCreatePage,
      ),
  },
  {
    path: ':topicName',
    data: { policyScope: 'topic' },
    loadComponent: () =>
      import('./feature/policy-list.page').then(
        module => module.PolicyListPage,
      ),
  },
];

export const PROJECT_POLICIES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    data: { directoryKind: 'projects' },
    loadComponent: () =>
      import('./feature/policy-directory-list.page').then(
        module => module.PolicyDirectoryListPage,
      ),
  },
  {
    path: 'new',
    data: { directoryKind: 'projects' },
    loadComponent: () =>
      import('./feature/policy-directory-create.page').then(
        module => module.PolicyDirectoryCreatePage,
      ),
  },
  {
    path: ':projectId/policies/new',
    data: { policyScope: 'project' },
    loadComponent: () =>
      import('./feature/policy-create.page').then(
        module => module.PolicyCreatePage,
      ),
  },
  {
    path: ':projectId',
    data: { policyScope: 'project' },
    loadComponent: () =>
      import('./feature/policy-list.page').then(
        module => module.PolicyListPage,
      ),
  },
];
