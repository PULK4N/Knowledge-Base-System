import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { SettingsService } from './core/settings/settings.service';
import { provideKnowledgeMarkdown } from './shared/markdown/markdown.providers';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(),
    provideAppInitializer(() => inject(SettingsService).load()),
    provideRouter(routes, withComponentInputBinding()),
    provideKnowledgeMarkdown(),
  ],
};
