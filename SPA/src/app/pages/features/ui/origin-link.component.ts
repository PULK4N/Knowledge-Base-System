import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OriginLink } from './specification-origin';

/** The "origin" link of a feature copy, opening the original item on its plan. */
@Component({
  selector: 'app-origin-link',
  imports: [RouterLink],
  template: `
    @let link = origin();
    @if (link) {
      <span class="origin-link">
        <span class="origin-label">Origin</span>
        @if (link.planId) {
          <a
            [routerLink]="['/features', featureId(), 'plans', link.planId]"
            [queryParams]="{ tab: link.tab }"
            [fragment]="link.fragment"
          >{{ link.text }}</a>
        } @else {
          <span>{{ link.text }}</span>
        }
      </span>
    }
  `,
  styles: `
    .origin-link {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
    }

    .origin-label {
      padding: 2px 7px;
      border-radius: 999px;
      color: #46526a;
      background: #e9edf4;
      font-weight: 700;
    }

    a {
      color: var(--indigo);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OriginLinkComponent {
  readonly featureId = input.required<string>();
  readonly origin = input<OriginLink | null>(null);
}
