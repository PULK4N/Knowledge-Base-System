import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Feature, FeaturePlan } from '../data-access/feature.models';
import { FeatureBranchOverviewComponent } from './feature-branch-overview.component';

const research: FeaturePlan = {
  id: 'research',
  title: 'RabbitMQ design',
  content: '',
  contentType: 'Markdown',
  additionalInfo: '',
  status: 'Completed',
  branch: { name: 'feature/research', description: '', subBranches: [] },
  previousPlanId: null,
  createdAt: '2026-10-01T00:00:00Z',
  updatedAt: '2026-10-01T00:00:00Z',
};
const implementation: FeaturePlan = {
  ...research,
  id: 'impl',
  title: 'Outbox processing',
  status: 'InProgress',
  branch: { name: 'feature/outbox', description: 'Dispatch', subBranches: [] },
  previousPlanId: 'research',
  createdAt: '2026-10-02T00:00:00Z',
};

describe('FeatureBranchOverviewComponent', () => {
  let fixture: ComponentFixture<FeatureBranchOverviewComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeatureBranchOverviewComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(FeatureBranchOverviewComponent);
    fixture.componentRef.setInput('feature', {
      id: 'feature-1',
      branch: { name: 'develop', description: 'Main line' },
      plans: [implementation, research],
    } as unknown as Feature);
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  });

  it('nests plan branches under the plan done before them with plan links', () => {
    expect(names()).toEqual(['develop', 'feature/research', 'feature/outbox']);
    const link = element.querySelector<HTMLAnchorElement>('.plan-link');
    expect(link?.getAttribute('href')).toBe('/features/feature-1/plans/research');
    expect(element.querySelectorAll('.plan-status')[1].textContent?.trim()).toBe(
      'In progress',
    );
  });

  it('collapses and expands every branch below the feature branch', () => {
    button('Collapse all')?.click();
    fixture.detectChanges();
    expect(names()).toEqual(['develop', 'feature/research']);

    button('Expand all')?.click();
    fixture.detectChanges();
    expect(names()).toContain('feature/outbox');
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

  function names(): string[] {
    return Array.from(element.querySelectorAll('.branch-row code')).map(
      code => code.textContent ?? '',
    );
  }

  function button(label: string): HTMLButtonElement | undefined {
    return Array.from(element.querySelectorAll<HTMLButtonElement>('button')).find(
      item => item.textContent?.trim() === label,
    );
  }
});
