import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideKnowledgeMarkdown } from '../../../shared/markdown/markdown.providers';
import { Feature } from '../data-access/feature.models';
import { FeatureBranchOverviewComponent } from './feature-branch-overview.component';

const feature = {
  id: 'feature-1',
  branch: { name: 'develop', description: 'Main line' },
  plans: [],
  branchGraph: [
    {
      key: 'feature',
      name: 'develop',
      description: 'Main line',
      planId: null,
      planTitle: null,
      planStatus: null,
      isBaseBranch: false,
      fromKeys: [],
    },
    {
      key: 'x1:x1-api',
      name: 'x1-api',
      description: '',
      planId: 'x1',
      planTitle: 'Outbox API',
      planStatus: 'InProgress',
      isBaseBranch: true,
      fromKeys: ['feature'],
    },
    {
      key: 'x3:x3-web',
      name: 'x3-web',
      description: '',
      planId: 'x3',
      planTitle: 'Outbox web',
      planStatus: 'Delayed',
      isBaseBranch: true,
      fromKeys: ['feature'],
    },
    {
      key: 'x2:x2',
      name: 'x2',
      description: 'Retry',
      planId: 'x2',
      planTitle: 'Retry policy',
      planStatus: 'NotStarted',
      isBaseBranch: true,
      fromKeys: ['x1:x1-api', 'x3:x3-web'],
    },
  ],
} as unknown as Feature;

describe('FeatureBranchOverviewComponent', () => {
  let fixture: ComponentFixture<FeatureBranchOverviewComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    Object.assign(globalThis, {
      mermaid: { initialize: vi.fn(), run: vi.fn() },
    });
    await TestBed.configureTestingModule({
      imports: [FeatureBranchOverviewComponent],
      providers: [provideRouter([]), ...provideKnowledgeMarkdown()],
    }).compileComponents();
    fixture = TestBed.createComponent(FeatureBranchOverviewComponent);
    fixture.componentRef.setInput('feature', feature);
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  });

  it('lists every branch with where it came from and links plans', () => {
    const lines = Array.from(element.querySelectorAll<HTMLElement>('.branch-graph-line'));

    expect(lines.map(line => line.querySelector('.branch-graph-name')?.textContent)).toEqual([
      'develop',
      'x1-api',
      'x3-web',
      'x2',
    ]);
    expect(lines[3].querySelector('.branch-graph-from')?.textContent).toContain('x1-api, x3-web');
    expect(lines[3].querySelector('.plan-link')?.getAttribute('href')).toBe(
      '/features/feature-1/plans/x2',
    );
    expect(lines[2].querySelector('.plan-status')?.textContent?.trim()).toBe('Delayed');
  });

  it('draws the graph from a Mermaid block', () => {
    expect(element.querySelector('app-markdown-content.branch-graph')).not.toBeNull();
  });

  it('emits the feature branch from the form', async () => {
    const branchSet = vi.fn();
    fixture.componentInstance.branchSet.subscribe(branchSet);

    button('Change feature branch')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    const input = element.querySelector<HTMLInputElement>('[name="featureBranchName"]')!;
    input.value = ' feature/outbox-main ';
    input.dispatchEvent(new Event('input'));
    element.querySelector('form')?.dispatchEvent(new Event('submit'));

    expect(branchSet).toHaveBeenCalledWith({
      branchName: 'feature/outbox-main',
      description: 'Main line',
    });
  });

  function button(label: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
      item => item.textContent?.trim() === label,
    );
  }
});
