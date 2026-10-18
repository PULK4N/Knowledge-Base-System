import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { UniqueConstraintRebuildResult } from '../data-access/unique-constraint-administration.models';
import { UniqueConstraintAdministrationService } from '../data-access/unique-constraint-administration.service';
import { UniqueConstraintRebuildPage } from './unique-constraint-rebuild.page';

describe('UniqueConstraintRebuildPage', () => {
  let fixture: ComponentFixture<UniqueConstraintRebuildPage>;
  let rebuildResult: Subject<UniqueConstraintRebuildResult>;
  let administration: {
    listStateMachines: ReturnType<typeof vi.fn>;
    rebuild: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    rebuildResult = new Subject<UniqueConstraintRebuildResult>();
    administration = {
      listStateMachines: vi.fn(() =>
        of(['features-state-machine', 'skills-state-machine']),
      ),
      rebuild: vi.fn(() => rebuildResult),
    };

    await TestBed.configureTestingModule({
      imports: [UniqueConstraintRebuildPage],
      providers: [
        {
          provide: UniqueConstraintAdministrationService,
          useValue: administration,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UniqueConstraintRebuildPage);
    fixture.detectChanges();
  });

  function select(stateMachineId: string): void {
    const element = fixture.nativeElement as HTMLElement;
    const selectElement = element.querySelector('select') as HTMLSelectElement;
    selectElement.value = stateMachineId;
    selectElement.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function button(): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector(
      'button',
    ) as HTMLButtonElement;
  }

  it('enables the rebuild only after a state machine is selected', () => {
    const options = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('option'),
    ).map(option => option.value);

    expect(options).toEqual([
      '',
      'features-state-machine',
      'skills-state-machine',
    ]);
    expect(button().disabled).toBe(true);

    select('skills-state-machine');

    expect(button().disabled).toBe(false);
  });

  it('rebuilds once per click and reports the stored counts', () => {
    select('skills-state-machine');

    button().click();
    button().click();
    fixture.detectChanges();

    expect(administration.rebuild).toHaveBeenCalledOnce();
    expect(administration.rebuild).toHaveBeenCalledWith('skills-state-machine');
    expect(button().textContent).toContain('Rebuilding…');

    rebuildResult.next({
      status: 'Completed',
      processedAggregateCount: 4,
      constraintCount: 3,
      conflicts: [],
    });
    rebuildResult.complete();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Stored 3 constraints for 4 aggregates.',
    );
    expect(button().disabled).toBe(false);
  });

  it('lists conflicting aggregates when values are duplicated', () => {
    select('skills-state-machine');
    button().click();

    rebuildResult.next({
      status: 'Conflicts',
      processedAggregateCount: 2,
      constraintCount: 2,
      conflicts: [
        {
          constraintName: 'UniqueSkillNameConstraint',
          aggregateIds: ['first-id', 'second-id'],
        },
      ],
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain('Nothing was written.');
    expect(text).toContain('UniqueSkillNameConstraint');
    expect(text).toContain('first-id');
    expect(text).toContain('second-id');
  });
});
