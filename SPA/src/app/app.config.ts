import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { authInterceptor } from './core/auth/auth.interceptor';
import { SettingsService } from './core/settings/settings.service';
import { provideKnowledgeMarkdown } from './shared/markdown/markdown.providers';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => inject(SettingsService).load()),
    provideRouter(routes, withComponentInputBinding()),
    provideKnowledgeMarkdown(),
  ],
};
