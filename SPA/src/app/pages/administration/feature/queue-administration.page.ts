import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmbeddedToolComponent } from '../../../shared/embedded-tool/embedded-tool.component';
import { QUEUE_TOOL } from '../data-access/embedded-tools';

@Component({
  selector: 'app-queue-administration-page',
  imports: [EmbeddedToolComponent],
  templateUrl: './queue-administration.page.html',
  styleUrl: './embedded-tool.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QueueAdministrationPage {
  protected readonly tool = QUEUE_TOOL;
}
