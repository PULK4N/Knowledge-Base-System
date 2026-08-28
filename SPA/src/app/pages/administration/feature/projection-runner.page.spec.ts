import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { ProjectionRunResult } from '../data-access/projection-administration.models';
import { ProjectionAdministrationService } from '../data-access/projection-administration.service';
import { ProjectionRunnerPage } from './projection-runner.page';

describe('ProjectionRunnerPage', () => {
  let fixture: ComponentFixture<ProjectionRunnerPage>;
  let runResult: Subject<ProjectionRunResult>;
  let administration: {
    list: ReturnType<typeof vi.fn>;
    run: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    runResult = new Subject<ProjectionRunResult>();
    administration = {
      list: vi.fn(() =>
        of([
          {
            stateMachineId: 'skills-state-machine',
            projectionNames: [
              'SkillSearchProjector',
              'SkillSummaryProjector',
            ],
          },
        ]),
      ),
      run: vi.fn(() => runResult),
    };

    await TestBed.configureTestingModule({
      imports: [ProjectionRunnerPage],
      providers: [
        {
          provide: ProjectionAdministrationService,
          useValue: administration,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectionRunnerPage);
    fixture.detectChanges();
  });

  it('runs a named projection for one aggregate without overlapping requests', () => {
    const element = fixture.nativeElement as HTMLElement;
    setInputValue(
      element.querySelector('[formControlName="projectionName"]'),
      'SkillSearchProjector',
    );
    setInputValue(
      element.querySelector('[formControlName="aggregateId"]'),
      'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    );
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(administration.run).toHaveBeenCalledOnce();
    expect(administration.run).toHaveBeenCalledWith({
      projectionName: 'SkillSearchProjector',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    });
    expect(element.textContent).toContain('Running…');

    runResult.next({ status: 'Completed', processedAggregateCount: 1 });
    runResult.complete();
    fixture.detectChanges();

    expect(element.textContent).toContain(
      'SkillSearchProjector completed',
    );
    expect(element.textContent).toContain('Processed 1 aggregate.');
  });

  it('runs the projection for all aggregates of a state machine', () => {
    const element = fixture.nativeElement as HTMLElement;
    setInputValue(
      element.querySelector('[formControlName="projectionName"]'),
      'SkillSummaryProjector',
    );
    const stateMachineScope = element.querySelector(
      'input[value="stateMachine"]',
    ) as HTMLInputElement;
    stateMachineScope.click();
    fixture.detectChanges();
    setInputValue(
      element.querySelector('[formControlName="stateMachineId"]'),
      'skills-state-machine',
    );
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));

    expect(administration.run).toHaveBeenCalledWith({
      projectionName: 'SkillSummaryProjector',
      stateMachineId: 'skills-state-machine',
    });
  });

  it('does not submit an invalid aggregate ID', () => {
    const element = fixture.nativeElement as HTMLElement;
    setInputValue(
      element.querySelector('[formControlName="projectionName"]'),
      'SkillSearchProjector',
    );
    setInputValue(
      element.querySelector('[formControlName="aggregateId"]'),
      'not-a-guid',
    );

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(administration.run).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Enter a valid aggregate GUID.');
  });
});

function setInputValue(
  input: Element | null,
  value: string,
): void {
  const element = input as HTMLInputElement;
  element.value = value;
  element.dispatchEvent(new Event('input'));
}
