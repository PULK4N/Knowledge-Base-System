import {
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';
import { MarkdownBlock } from './markdown-blocks';

@Component({
  selector: 'app-markdown-content',
  templateUrl: './markdown-content.component.html',
  styleUrl: './markdown-content.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkdownContentComponent {
  readonly blocks = input.required<readonly MarkdownBlock[]>();
  readonly emptyMessage = input('There is no content to display.');
}
