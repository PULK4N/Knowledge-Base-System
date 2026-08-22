import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  ViewEncapsulation,
} from '@angular/core';
import { Router } from '@angular/router';
import { MarkdownComponent } from 'ngx-markdown';
import { MarkdownBlock } from './markdown-blocks';

export function qualifyMarkdownFragmentLinks(
  container: ParentNode,
  routeUrl: string,
): void {
  const routeWithoutFragment = routeUrl.split('#', 1)[0] || '/';

  container
    .querySelectorAll<HTMLAnchorElement>('a[href^="#"]')
    .forEach((link) => {
      const fragment = link.getAttribute('href');
      if (fragment) {
        link.setAttribute('href', `${routeWithoutFragment}${fragment}`);
      }
    });
}

@Component({
  selector: 'app-markdown-content',
  imports: [MarkdownComponent],
  templateUrl: './markdown-content.component.html',
  styleUrl: './markdown-content.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class MarkdownContentComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly router = inject(Router);

  readonly blocks = input.required<readonly MarkdownBlock[]>();
  readonly markdown = input<string>();
  readonly emptyMessage = input('There is no content to display.');

  protected readonly hasMarkdown = computed(
    () => (this.markdown()?.trim().length ?? 0) > 0,
  );

  protected qualifyFragmentLinks(): void {
    qualifyMarkdownFragmentLinks(this.host.nativeElement, this.router.url);
  }
}
