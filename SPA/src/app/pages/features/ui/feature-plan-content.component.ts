import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import { FeaturePlan } from '../data-access/feature.models';

@Component({
  selector: 'app-feature-plan-content',
  imports: [MarkdownContentComponent],
  templateUrl: './feature-plan-content.component.html',
  styleUrl: './feature-plan-content.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlanContentComponent {
  readonly plan = input.required<FeaturePlan>();
  protected readonly emptyBlocks = [];
}
