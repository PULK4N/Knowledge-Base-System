import { TestBed } from '@angular/core/testing';
import { FeaturePlan } from '../data-access/feature.models';
import { FeaturePlanContentComponent } from './feature-plan-content.component';

const htmlPlan: FeaturePlan = {
  id: 'plan-1',
  title: 'Styled plan',
  content:
    '<!doctype html><html><head><style>body { color: tomato; }</style></head><body><h1>Plan</h1></body></html>',
  contentType: 'Html',
  createdAt: '2026-08-26T10:00:00Z',
  updatedAt: '2026-08-26T10:00:00Z',
};

describe('FeaturePlanContentComponent', () => {
  it('renders a complete HTML plan in a sandboxed iframe', async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturePlanContentComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FeaturePlanContentComponent);
    fixture.componentRef.setInput('plan', htmlPlan);
    fixture.detectChanges();

    const iframe = fixture.nativeElement.querySelector(
      'iframe.html-plan',
    ) as HTMLIFrameElement | null;

    expect(iframe).not.toBeNull();
    expect(iframe?.srcdoc).toBe(htmlPlan.content);
    expect(iframe?.getAttribute('sandbox')).toBe('');
    expect(iframe?.getAttribute('referrerpolicy')).toBe('no-referrer');
    expect(iframe?.title).toBe('HTML plan: Styled plan');
    expect(fixture.nativeElement.querySelector('article.html-plan')).toBeNull();
  });

  it('does not create an iframe for an empty HTML plan', async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturePlanContentComponent],
    }).compileComponents();

    const fixture = TestBed.createComponent(FeaturePlanContentComponent);
    fixture.componentRef.setInput('plan', { ...htmlPlan, content: '   ' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('iframe')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain(
      'This plan has no content.',
    );
  });
});
