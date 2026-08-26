import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { FeaturePlan } from '../data-access/feature.models';

const PREVIEW_URL_LIFETIME_MS = 60_000;

@Injectable({ providedIn: 'root' })
export class HtmlPlanNewTabService {
  private readonly document = inject(DOCUMENT);

  open(plan: FeaturePlan): void {
    const browserWindow = this.document.defaultView;
    if (!browserWindow) return;

    const previewUrl = browserWindow.URL.createObjectURL(
      new Blob([plan.content], { type: 'text/html;charset=utf-8' }),
    );

    browserWindow.open(previewUrl, '_blank', 'noopener,noreferrer');
    browserWindow.setTimeout(
      () => browserWindow.URL.revokeObjectURL(previewUrl),
      PREVIEW_URL_LIFETIME_MS,
    );
  }
}
