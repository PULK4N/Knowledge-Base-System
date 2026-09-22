import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-memories-tabs',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './memories-tabs.component.html',
  styleUrl: './memories-tabs.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemoriesTabsComponent {
  protected readonly exactMatch = { exact: true };
}
