import { Routes } from '@angular/router';

export const POLICIES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./feature/policies.page').then(module => module.PoliciesPage),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'general',
      },
      {
        path: 'general',
        pathMatch: 'full',
        data: { policyScope: 'general' },
        loadComponent: () =>
          import('./feature/policy-list.page').then(
            module => module.PolicyListPage,
          ),
      },
      {
        path: 'general/new',
        data: { policyScope: 'general' },
        loadComponent: () =>
          import('./feature/policy-create.page').then(
            module => module.PolicyCreatePage,
          ),
      },
      {
        path: 'topics',
        pathMatch: 'full',
        data: { directoryKind: 'topics' },
        loadComponent: () =>
          import('./feature/policy-directory-list.page').then(
            module => module.PolicyDirectoryListPage,
          ),
      },
      {
        path: 'topics/new',
        data: { directoryKind: 'topics' },
        loadComponent: () =>
          import('./feature/policy-directory-create.page').then(
            module => module.PolicyDirectoryCreatePage,
          ),
      },
      {
        path: 'topics/:topicName/policies/new',
        data: { policyScope: 'topic' },
        loadComponent: () =>
          import('./feature/policy-create.page').then(
            module => module.PolicyCreatePage,
          ),
      },
      {
        path: 'topics/:topicName',
        data: { policyScope: 'topic' },
        loadComponent: () =>
          import('./feature/policy-list.page').then(
            module => module.PolicyListPage,
          ),
      },
      {
        path: 'agent-families',
        pathMatch: 'full',
        data: { directoryKind: 'agent-families' },
        loadComponent: () =>
          import('./feature/policy-directory-list.page').then(
            module => module.PolicyDirectoryListPage,
          ),
      },
      {
        path: 'agent-families/new',
        data: { directoryKind: 'agent-families' },
        loadComponent: () =>
          import('./feature/policy-directory-create.page').then(
            module => module.PolicyDirectoryCreatePage,
          ),
      },
      {
        path: 'agent-families/:agentFamilyName/policies/new',
        data: { policyScope: 'agentFamily' },
        loadComponent: () =>
          import('./feature/policy-create.page').then(
            module => module.PolicyCreatePage,
          ),
      },
      {
        path: 'agent-families/:agentFamilyName',
        data: { policyScope: 'agentFamily' },
        loadComponent: () =>
          import('./feature/policy-list.page').then(
            module => module.PolicyListPage,
          ),
      },
      {
        path: 'projects',
        pathMatch: 'full',
        data: { directoryKind: 'projects' },
        loadComponent: () =>
          import('./feature/policy-directory-list.page').then(
            module => module.PolicyDirectoryListPage,
          ),
      },
      {
        path: 'projects/new',
        data: { directoryKind: 'projects' },
        loadComponent: () =>
          import('./feature/policy-directory-create.page').then(
            module => module.PolicyDirectoryCreatePage,
          ),
      },
      {
        path: 'projects/:projectId/policies/new',
        data: { policyScope: 'project' },
        loadComponent: () =>
          import('./feature/policy-create.page').then(
            module => module.PolicyCreatePage,
          ),
      },
      {
        path: 'projects/:projectId',
        data: { policyScope: 'project' },
        loadComponent: () =>
          import('./feature/policy-list.page').then(
            module => module.PolicyListPage,
          ),
      },
    ],
  },
];
