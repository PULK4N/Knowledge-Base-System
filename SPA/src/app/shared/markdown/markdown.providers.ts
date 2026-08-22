import { Provider } from '@angular/core';
import DOMPurify from 'dompurify';
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
