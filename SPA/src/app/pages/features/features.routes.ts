import { Routes } from '@angular/router';

export const FEATURES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/features-list.page').then(
        module => module.FeaturesListPage,
      ),
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./feature/feature-create.page').then(
        module => module.FeatureCreatePage,
      ),
  },
  {
    path: ':featureId/plans/:planId',
    loadComponent: () =>
      import('./feature/feature-plan-details.page').then(
        module => module.FeaturePlanDetailsPage,
      ),
  },
  {
    path: ':featureId',
    loadComponent: () =>
      import('./feature/feature-details.page').then(
        module => module.FeatureDetailsPage,
      ),
  },
];
