import { Provider } from '@angular/core';
import DOMPurify from 'dompurify';
import { Token } from 'marked';
import markedAlert from 'marked-alert';
import markedFootnote from 'marked-footnote';
import { gfmHeadingId } from 'marked-gfm-heading-id';
import {
  MARKED_EXTENSIONS,
  MARKED_OPTIONS,
  MERMAID_OPTIONS,
  SANITIZE,
  provideMarkdown,
} from 'ngx-markdown';

const README_CODE_LANGUAGE_ALIASES: Readonly<Record<string, string>> = {
  'c#': 'csharp',
  'c-sharp': 'csharp',
};

export function normalizeMarkdownCodeLanguage(token: Token): void {
  if (token.type !== 'code' || !token.lang) {
    return;
  }

  const [language, ...metadata] = token.lang.trim().split(/\s+/);
  const normalizedLanguage = README_CODE_LANGUAGE_ALIASES[language.toLowerCase()];

  if (normalizedLanguage) {
    token.lang = [normalizedLanguage, ...metadata].join(' ');
  }
}

function sanitizeMarkdownHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    USE_PROFILES: { html: true },
  });
}

export function provideKnowledgeMarkdown(): Provider[] {
  return provideMarkdown({
    markedOptions: {
      provide: MARKED_OPTIONS,
      useValue: {
        breaks: false,
        gfm: true,
        pedantic: false,
      },
    },
    markedExtensions: [
      {
        provide: MARKED_EXTENSIONS,
        useFactory: gfmHeadingId,
        multi: true,
      },
      {
        provide: MARKED_EXTENSIONS,
        useFactory: markedAlert,
        multi: true,
      },
      {
        provide: MARKED_EXTENSIONS,
        useFactory: markedFootnote,
        multi: true,
      },
      {
        provide: MARKED_EXTENSIONS,
        useValue: {
          walkTokens: normalizeMarkdownCodeLanguage,
        },
        multi: true,
      },
    ],
    mermaidOptions: {
      provide: MERMAID_OPTIONS,
      useValue: {
        securityLevel: 'strict',
        startOnLoad: false,
        theme: 'neutral',
      },
    },
    sanitize: {
      provide: SANITIZE,
      useValue: sanitizeMarkdownHtml,
    },
  });
}
