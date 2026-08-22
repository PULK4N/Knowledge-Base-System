import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  ViewEncapsulation,
} from '@angular/core';
import { MarkdownComponent } from 'ngx-markdown';
import { MarkdownBlock } from './markdown-blocks';

@Component({
  selector: 'app-markdown-content',
  imports: [MarkdownComponent],
  templateUrl: './markdown-content.component.html',
  styleUrl: './markdown-content.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class MarkdownContentComponent {
  readonly blocks = input.required<readonly MarkdownBlock[]>();
  readonly markdown = input<string>();
  readonly emptyMessage = input('There is no content to display.');

  protected readonly hasMarkdown = computed(
    () => (this.markdown()?.trim().length ?? 0) > 0,
  );
}
