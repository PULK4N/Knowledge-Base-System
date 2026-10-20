import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject } from 'rxjs';
import { provideKnowledgeMarkdown } from '../../../shared/markdown/markdown.providers';
import { Feature, FeaturePlan } from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import { FeaturePlanDetailsPage } from './feature-plan-details.page';

const plan: FeaturePlan = {
  id: 'x2', title: 'X2', content: '# Plan', contentType: 'Markdown',
  additionalInfo: '', status: 'InProgress', previousPlanId: 'x1', recordIds: [],
  branches: [{ name: 'x2', description: '', sources: [], subBranches: [] }],
  createdAt: '2026-10-20T10:00:00Z', updatedAt: '2026-10-20T10:00:00Z',
};
const feature: Feature = {
  id: 'feature-1', isDeleted: false, projectId: 'project-1', name: 'Feature',
  summary: '', status: '', planCount: 2, recordCount: 0, relatedSkillIds: [],
  records: [], reviewNotes: [], branch: { name: 'main', description: '' },
  branchGraph: [], memoryHistory: [],
  plans: [
    { ...plan, id: 'x1', title: 'X1', previousPlanId: null,
      branches: ['x1-api', 'x1-web'].map(name => ({
        name, description: '', sources: [], subBranches: [],
      })),
    },
    plan,
  ],
};

describe('FeaturePlanDetailsPage completion', () => {
  let harness: RouterTestingHarness;
  let result: Subject<Feature>;
  let changePlanStatus: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    Object.assign(globalThis, { mermaid: { initialize: vi.fn(), run: vi.fn() } });
    result = new Subject<Feature>();
    changePlanStatus = vi.fn(() => result);
    await TestBed.configureTestingModule({
      imports: [FeaturePlanDetailsPage],
      providers: [
        provideRouter([{ path: 'features/:featureId/plans/:planId', component: FeaturePlanDetailsPage }]),
        ...provideKnowledgeMarkdown(),
        { provide: FeatureService, useValue: { watch: () => of(feature), changePlanStatus } },
      ],
    }).compileComponents();
    harness = await RouterTestingHarness.create('/features/feature-1/plans/x2');
    await settle();
  });

  it('keeps the selected merge target on failure and closes after a successful retry', async () => {
    await select('planStatus', 'Completed');
    expect(changePlanStatus).not.toHaveBeenCalled();
    await select('mergedInto-x2', 'x1:x1-web');
    button('Complete plan').click();
    await settle();

    expect(changePlanStatus).toHaveBeenCalledWith('feature-1', 'x2', 'Completed', [
      { branchName: 'x2', intoPlanId: 'x1', intoBranchName: 'x1-web' },
    ]);
    expect(control('mergedInto-x2').disabled).toBe(true);
    expect(button('Cancel').disabled).toBe(true);

    result.error(new Error('Save failed'));
    await settle();
    expect(control('mergedInto-x2').value).toBe('x1:x1-web');
    expect(element().querySelector('[role="alert"]')?.textContent?.trim()).toBeTruthy();

    result = new Subject<Feature>();
    button('Complete plan').click();
    result.next(feature);
    result.complete();
    await settle();
    expect(element().querySelector('.plan-completion')).toBeNull();
    expect(changePlanStatus).toHaveBeenCalledTimes(2);
  });

  it('cancels completion without sending a command', async () => {
    await select('planStatus', 'Completed');
    button('Cancel').click();
    await settle();
    expect(element().querySelector('.plan-completion')).toBeNull();
    expect(control('planStatus').value).toBe('InProgress');
    expect(changePlanStatus).not.toHaveBeenCalled();
  });

  it('closes completion when selecting another status', async () => {
    await select('planStatus', 'Completed');
    await select('planStatus', 'Delayed');
    expect(element().querySelector('.plan-completion')).toBeNull();
    expect(changePlanStatus).toHaveBeenCalledWith('feature-1', 'x2', 'Delayed', []);
  });

  it('clears pending merge choices when navigating to another plan', async () => {
    await select('planStatus', 'Completed');
    await select('mergedInto-x2', 'x1:x1-web');
    await harness.navigateByUrl('/features/feature-1/plans/x1');
    await settle();
    expect(element().querySelector('.plan-completion')).toBeNull();
    expect(changePlanStatus).not.toHaveBeenCalled();
  });

  function element(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function control(name: string): HTMLSelectElement {
    return element().querySelector(name.startsWith('mergedInto-')
      ? '.merge-target select'
      : `select[name="${name}"]`)!;
  }

  function button(text: string): HTMLButtonElement {
    return [...element().querySelectorAll('button')].find(item => item.textContent?.trim() === text)!;
  }

  async function select(name: string, value: string): Promise<void> {
    control(name).value = value;
    control(name).dispatchEvent(new Event('change'));
    await settle();
  }

  async function settle(): Promise<void> {
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();
  }
});
