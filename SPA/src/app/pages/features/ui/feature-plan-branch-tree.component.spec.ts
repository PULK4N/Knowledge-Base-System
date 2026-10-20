import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeaturePlanBranch } from '../data-access/feature.models';
import { FeaturePlanBranchTreeComponent } from './feature-plan-branch-tree.component';

const base: FeaturePlanBranch = {
  name: 'feature/plans',
  description: 'Plan rework',
  subBranches: [
    {
      name: 'feature/plans-api',
      description: 'Backend',
      subBranches: [
        { name: 'feature/plans-api-tests', description: '', subBranches: [], sources: [] },
      ],
      sources: [],
    },
    { name: 'feature/plans-ui', description: '', subBranches: [], sources: [] },
  ],
  sources: [],
};
const secondBase: FeaturePlanBranch = {
  name: 'feature/plans-web',
  description: '',
  subBranches: [],
  sources: [{ planId: 'x1', branchName: 'x1-api' }],
};
const sourceOptions = [
  { id: 'feature', label: 'main', detail: 'feature branch' },
  { id: 'x1:x1-api', label: 'x1-api', detail: 'Plan X1' },
  { id: 'x3:x3-web', label: 'x3-web', detail: 'Plan X3' },
];

describe('FeaturePlanBranchTreeComponent', () => {
  let fixture: ComponentFixture<FeaturePlanBranchTreeComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturePlanBranchTreeComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(FeaturePlanBranchTreeComponent);
    fixture.componentRef.setInput('branches', [base]);
    fixture.componentRef.setInput('sourceOptions', sourceOptions);
    fixture.componentRef.setInput(
      'describeSources',
      (branch: FeaturePlanBranch) =>
        branch.sources.map(source => source.branchName).join(', ') || 'main',
    );
    fixture.detectChanges();
    element = fixture.nativeElement as HTMLElement;
  });

  it('starts with only the base branch open and marks only the base branch', () => {
    expect(branchNames()).toEqual([
      'feature/plans',
      'feature/plans-api',
      'feature/plans-ui',
    ]);
    expect(rowFor('feature/plans-api')?.querySelector('small')?.textContent).toBe(
      '1 hidden',
    );
    expect(element.querySelectorAll('.base-badge')).toHaveLength(1);
    expect(element.querySelector('.count-badge')?.textContent?.trim()).toBe('4');
  });

  it('opens and collapses a branch and all branches', () => {
    toggleFor('feature/plans-api')?.click();
    fixture.detectChanges();

    expect(branchNames()).toContain('feature/plans-api-tests');
    expect(toggleFor('feature/plans-api')?.getAttribute('aria-label')).toBe(
      'Collapse feature/plans-api',
    );

    button(element, 'Collapse all')?.click();
    fixture.detectChanges();
    expect(branchNames()).not.toContain('feature/plans-api-tests');

    button(element, 'Expand all')?.click();
    fixture.detectChanges();
    expect(branchNames()).toHaveLength(4);

    toggleFor('feature/plans')?.click();
    fixture.detectChanges();
    expect(branchNames()).toEqual(['feature/plans']);
  });

  it('offers removal for sub-branches only and emits after confirmation', () => {
    const removed = vi.fn();
    fixture.componentInstance.branchRemoved.subscribe(removed);
    const [baseRow, apiRow] = rows();

    expect(button(baseRow, 'Remove')).toBeUndefined();

    button(apiRow, 'Remove')?.click();
    fixture.detectChanges();
    expect(removed).not.toHaveBeenCalled();

    button(rows()[1], 'Confirm remove')?.click();
    expect(removed).toHaveBeenCalledWith('feature/plans-api');
  });

  it('emits a sub-branch request under the chosen parent', async () => {
    const added = vi.fn();
    fixture.componentInstance.branchAdded.subscribe(added);

    button(rowFor('feature/plans-ui')!, 'Add sub-branch')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    setValue('.branch-form [name="name"]', ' feature/plans-ui-tests ');
    setValue('.branch-form [name="description"]', 'UI tests');
    element.querySelector('.branch-form')?.dispatchEvent(new Event('submit'));

    expect(added).toHaveBeenCalledWith({
      branchName: 'feature/plans-ui-tests',
      description: 'UI tests',
      parentBranchName: 'feature/plans-ui',
      sources: [],
    });
  });

  it('emits a rename for the base branch', async () => {
    const updated = vi.fn();
    fixture.componentInstance.branchUpdated.subscribe(updated);

    button(rows()[0], 'Edit')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    setValue('.branch-form [name="name"]', 'feature/plan-storage');
    element.querySelector('.branch-form')?.dispatchEvent(new Event('submit'));

    expect(updated).toHaveBeenCalledWith({
      branchName: 'feature/plans',
      newBranchName: 'feature/plan-storage',
      description: 'Plan rework',
    });
  });

  it('offers to add a base branch to a plan without one', () => {
    fixture.componentRef.setInput('branches', []);
    fixture.detectChanges();

    expect(element.querySelector('.branch-tree')).toBeNull();
    expect(button(element, 'Add base branch')).toBeDefined();
  });

  it('shows every base branch with its sources and lets a base branch be removed when another remains', () => {
    fixture.componentRef.setInput('branches', [base, secondBase]);
    fixture.detectChanges();

    expect(element.querySelectorAll('.base-badge')).toHaveLength(2);
    expect(rowFor('feature/plans')?.querySelector('.branch-sources')?.textContent).toContain('main');
    expect(rowFor('feature/plans-web')?.querySelector('.branch-sources')?.textContent).toContain('x1-api');
    expect(button(rowFor('feature/plans')!, 'Remove')).toBeDefined();
  });

  it('adds another base branch created from branches of two plans', async () => {
    const added = vi.fn();
    fixture.componentInstance.branchAdded.subscribe(added);

    button(element, 'Add base branch')?.click();
    fixture.detectChanges();
    await fixture.whenStable();
    setValue('.branch-form [name="name"]', ' x2 ');
    pick('x1-api');
    fixture.detectChanges();
    pick('x3-web');
    fixture.detectChanges();
    element.querySelector('.branch-form')?.dispatchEvent(new Event('submit'));

    expect(added).toHaveBeenCalledWith({
      branchName: 'x2',
      description: '',
      parentBranchName: null,
      sources: [
        { planId: 'x1', branchName: 'x1-api' },
        { planId: 'x3', branchName: 'x3-web' },
      ],
    });
  });

  it('edits the sources of a base branch starting from the current ones', () => {
    const sourcesSet = vi.fn();
    fixture.componentInstance.sourcesSet.subscribe(sourcesSet);
    fixture.componentRef.setInput('branches', [base, secondBase]);
    fixture.detectChanges();

    button(rowFor('feature/plans-web')!, 'Sources')?.click();
    fixture.detectChanges();
    pick('main');
    fixture.detectChanges();
    button(element, 'Save sources')?.click();

    expect(sourcesSet).toHaveBeenCalledWith({
      branchName: 'feature/plans-web',
      sources: [
        { planId: 'x1', branchName: 'x1-api' },
        { planId: null, branchName: '' },
      ],
    });
  });

  function pick(label: string): void {
    Array.from(element.querySelectorAll<HTMLElement>('.link-option'))
      .find(option => option.querySelector('.link-label')?.textContent === label)
      ?.querySelector<HTMLInputElement>('input')
      ?.click();
  }

  function rows(): HTMLElement[] {
    return Array.from(element.querySelectorAll<HTMLElement>('.branch-row'));
  }

  function branchNames(): string[] {
    return rows().map(row => row.querySelector('code')?.textContent ?? '');
  }

  function rowFor(name: string): HTMLElement | undefined {
    return rows().find(row => row.querySelector('code')?.textContent === name);
  }

  function toggleFor(name: string): HTMLButtonElement | null | undefined {
    return rowFor(name)?.querySelector<HTMLButtonElement>('.branch-toggle');
  }

  function setValue(selector: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }
});

function button(
  container: HTMLElement,
  label: string,
): HTMLButtonElement | undefined {
  return Array.from(container.querySelectorAll<HTMLButtonElement>('button')).find(
    item => item.textContent?.trim() === label,
  );
}
