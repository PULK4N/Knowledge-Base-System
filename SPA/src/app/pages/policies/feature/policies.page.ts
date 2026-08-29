import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-policies-page',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './policies.page.html',
  styleUrl: './policies.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PoliciesPage {}
