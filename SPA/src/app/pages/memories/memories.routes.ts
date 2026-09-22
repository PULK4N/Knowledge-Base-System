import { Routes } from '@angular/router';

export const MEMORIES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/memories-list.page').then(
        module => module.MemoriesListPage,
      ),
  },
  {
    path: 'tool-calls',
    pathMatch: 'full',
    loadComponent: () =>
      import('./feature/memory-tool-calls.page').then(
        module => module.MemoryToolCallsPage,
      ),
  },
  {
    path: ':memoryId',
    loadComponent: () =>
      import('./feature/memory-chat.page').then(
        module => module.MemoryChatPage,
      ),
  },
];
