import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmbeddedToolComponent } from '../../../shared/embedded-tool/embedded-tool.component';
import { DATABASE_TOOL } from '../data-access/embedded-tools';

@Component({
  selector: 'app-database-administration-page',
  imports: [EmbeddedToolComponent],
  templateUrl: './database-administration.page.html',
  styleUrl: './embedded-tool.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DatabaseAdministrationPage {
  protected readonly tool = DATABASE_TOOL;
}
