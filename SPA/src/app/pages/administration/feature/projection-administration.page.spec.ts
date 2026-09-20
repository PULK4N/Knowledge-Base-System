import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { ProjectionReplayQueuedResult } from '../data-access/projection-administration.models';
import { ProjectionAdministrationService } from '../data-access/projection-administration.service';
import { ProjectionAdministrationPage } from './projection-administration.page';

describe('ProjectionAdministrationPage', () => {
  let fixture: ComponentFixture<ProjectionAdministrationPage>;
  let replayResult: Subject<ProjectionReplayQueuedResult>;
  let administration: {
    list: ReturnType<typeof vi.fn>;
    execute: ReturnType<typeof vi.fn>;
    run: ReturnType<typeof vi.fn>;
    clearEmbeddingCache: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    replayResult = new Subject<ProjectionReplayQueuedResult>();
    administration = {
      list: vi.fn(() =>
        of([
          {
            stateMachineId: 'skill-state-machine',
            projectionNames: [
              'SkillSearchProjector',
              'SkillSummaryProjector',
            ],
          },
        ]),
      ),
      execute: vi.fn(() => replayResult),
      run: vi.fn(),
      clearEmbeddingCache: vi.fn(() => of(3)),
    };

    await TestBed.configureTestingModule({
      imports: [ProjectionAdministrationPage],
      providers: [
        {
          provide: ProjectionAdministrationService,
          useValue: administration,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectionAdministrationPage);
    fixture.detectChanges();
  });

  it('lists projector names and reports the queued aggregate count', () => {
    const element = fixture.nativeElement as HTMLElement;
    const button = element.querySelector('button') as HTMLButtonElement;

    expect(element.textContent).toContain('skill-state-machine');
    expect(element.textContent).toContain('SkillSearchProjector');
    expect(element.textContent).toContain('SkillSummaryProjector');

    button.click();
    button.click();
    fixture.detectChanges();

    expect(administration.execute).toHaveBeenCalledOnce();
    expect(administration.execute).toHaveBeenCalledWith(
      'skill-state-machine',
    );
    expect(button.textContent).toContain('Queuing…');
    expect(button.disabled).toBe(true);

    replayResult.next({ status: 'Queued', queuedAggregateCount: 2 });
    replayResult.complete();
    fixture.detectChanges();

    expect(element.textContent).toContain('Queued 2 aggregates.');
    expect(button.disabled).toBe(false);
  });

  it('offers the loaded projector names to the single-projection runner', () => {
    const element = fixture.nativeElement as HTMLElement;
    const suggestions = Array.from(
      element.querySelectorAll('#projection-names option'),
    ).map(option => (option as HTMLOptionElement).value);

    expect(element.textContent).toContain('Run one projection');
    expect(suggestions).toEqual([
      'SkillSearchProjector',
      'SkillSummaryProjector',
    ]);
  });

  it('clears the embedding cache and reports the deleted count', () => {
    const element = fixture.nativeElement as HTMLElement;
    const button = Array.from(element.querySelectorAll('button')).find(
      candidate => candidate.textContent?.includes('Clear embedding cache'),
    ) as HTMLButtonElement;

    button.click();
    fixture.detectChanges();

    expect(administration.clearEmbeddingCache).toHaveBeenCalledOnce();
    expect(element.textContent).toContain('Cleared 3 cached embeddings.');
  });
});
