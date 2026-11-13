import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Lists missing links in a specification, such as "FR-2 has no definition of done". */
@Component({
  selector: 'app-specification-warnings',
  template: `
    @if (warnings().length > 0) {
      <details class="warnings" role="status">
        <summary>
          <span aria-hidden="true">⚠</span>
          {{ warnings().length }} missing {{ warnings().length === 1 ? 'link' : 'links' }}
        </summary>
        <ul>
          @for (warning of warnings(); track warning) {
            <li>{{ warning }}</li>
          }
        </ul>
      </details>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .warnings {
      padding: 10px 14px;
      border: 1px solid #f1d39b;
      border-radius: 10px;
      color: #72441c;
      background: #fff8ea;
      font-size: 13px;
    }

    summary {
      cursor: pointer;
      font-weight: 700;
    }

    ul {
      margin: 8px 0 0;
      padding-left: 20px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpecificationWarningsComponent {
  readonly warnings = input<readonly string[]>([]);
}
