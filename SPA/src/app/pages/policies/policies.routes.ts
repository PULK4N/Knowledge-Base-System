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
    path: ':projectId',
    data: { policyScope: 'project' },
    loadComponent: () =>
      import('./feature/policy-list.page').then(
        module => module.PolicyListPage,
      ),
  },
];
