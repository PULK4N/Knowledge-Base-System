import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { FeaturePlan } from '../data-access/feature.models';
import { HtmlPlanNewTabService } from './html-plan-new-tab.service';

describe('HtmlPlanNewTabService', () => {
  it('opens the plan HTML itself as a new top-level document', async () => {
    const plan: FeaturePlan = {
      id: 'plan-1',
      title: 'Plan one',
      content: '<!doctype html><html><body><h1>Plan</h1></body></html>',
      contentType: 'Html',
      createdAt: '2026-08-26T10:00:00Z',
      updatedAt: '2026-08-26T10:00:00Z',
    };
    const createObjectURL = vi.fn().mockReturnValue('blob:plan-preview');
    const revokeObjectURL = vi.fn();
    const open = vi.fn();
    const setTimeout = vi.fn();
    const browserWindow = {
      URL: { createObjectURL, revokeObjectURL },
      open,
      setTimeout,
    } as unknown as Window;

    TestBed.configureTestingModule({
      providers: [
        HtmlPlanNewTabService,
        {
          provide: DOCUMENT,
          useValue: { defaultView: browserWindow },
        },
      ],
    });

    TestBed.inject(HtmlPlanNewTabService).open(plan);

    const blob = createObjectURL.mock.calls[0][0] as Blob;
    expect(await blob.text()).toBe(plan.content);
    expect(blob.type).toBe('text/html;charset=utf-8');
    expect(open).toHaveBeenCalledWith(
      'blob:plan-preview',
      '_blank',
      'noopener,noreferrer',
    );

    const revoke = setTimeout.mock.calls[0][0] as () => void;
    revoke();

    expect(revokeObjectURL).toHaveBeenCalledWith('blob:plan-preview');
  });
});
