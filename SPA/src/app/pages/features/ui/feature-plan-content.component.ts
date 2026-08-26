import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
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
  private readonly sanitizer = inject(DomSanitizer);

  readonly plan = input.required<FeaturePlan>();
  protected readonly emptyBlocks = [];
  // The empty iframe sandbox is the security boundary; trusting srcdoc here
  // preserves complete documents and embedded styles without exposing the host DOM.
  protected readonly sandboxedHtmlDocument = computed(() =>
    this.sanitizer.bypassSecurityTrustHtml(this.plan().content),
  );
}
