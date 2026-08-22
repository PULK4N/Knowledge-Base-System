import { Provider } from '@angular/core';
import markedAlert from 'marked-alert';
import markedFootnote from 'marked-footnote';
import { gfmHeadingId } from 'marked-gfm-heading-id';
import {
  MARKED_EXTENSIONS,
  MARKED_OPTIONS,
  MERMAID_OPTIONS,
  provideMarkdown,
} from 'ngx-markdown';

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
  });
}
