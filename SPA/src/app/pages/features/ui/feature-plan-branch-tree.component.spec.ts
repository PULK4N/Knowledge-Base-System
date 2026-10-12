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
        { name: 'feature/plans-api-tests', description: '', subBranches: [] },
      ],
    },
    { name: 'feature/plans-ui', description: '', subBranches: [] },
  ],
};

describe('FeaturePlanBranchTreeComponent', () => {
  let fixture: ComponentFixture<FeaturePlanBranchTreeComponent>;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturePlanBranchTreeComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(FeaturePlanBranchTreeComponent);
    fixture.componentRef.setInput('branch', base);
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
    fixture.componentRef.setInput('branch', null);
    fixture.detectChanges();

    expect(element.querySelector('.branch-tree')).toBeNull();
    expect(button(element, 'Add base branch')).toBeDefined();
  });

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
