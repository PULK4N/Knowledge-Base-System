import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-administration-page',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './administration.page.html',
  styleUrl: './administration.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdministrationPage {}
