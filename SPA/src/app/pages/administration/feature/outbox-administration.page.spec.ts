import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { OutboxPayload } from '../data-access/outbox-administration.models';
import { OutboxAdministrationService } from '../data-access/outbox-administration.service';
import { OutboxAdministrationPage } from './outbox-administration.page';

describe('OutboxAdministrationPage', () => {
  let fixture: ComponentFixture<OutboxAdministrationPage>;
  let requeueResult: Subject<OutboxPayload>;
  let administration: {
    search: ReturnType<typeof vi.fn>;
    requeue: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    requeueResult = new Subject<OutboxPayload>();
    administration = {
      search: vi.fn(() =>
        of({
          items: [
            {
              id: '17',
              payloadId: 17,
              state: 'Error',
              retryCount: 3,
              errorMessage: 'Projection failed.',
              stateMachineId: 'skills-state-machine',
              aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
              orderNumber: 7,
              eventName: 'SkillUpdatedV1',
              timestamp: '2026-08-22T12:00:00Z',
              executionInfoJson:
                '{\n  "eventName": "SkillUpdatedV1"\n}',
              eventDataJson: '{\n  "name": "Updated skill"\n}',
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        }),
      ),
      requeue: vi.fn(() => requeueResult),
    };

    await TestBed.configureTestingModule({
      imports: [OutboxAdministrationPage],
      providers: [
        {
          provide: OutboxAdministrationService,
          useValue: administration,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OutboxAdministrationPage);
    fixture.detectChanges();
  });

  it('filters incomplete payloads and requeues without duplicate submits', () => {
    const element = fixture.nativeElement as HTMLElement;
    const checkbox = element.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    expect(element.textContent).toContain('SkillUpdatedV1');
    expect(element.textContent).toContain('Projection failed.');

    checkbox.click();
    fixture.detectChanges();
    expect(administration.search).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 10,
      onlyIncomplete: true,
    });

    const button = element.querySelector(
      '.row-action button',
    ) as HTMLButtonElement;
    button.click();
    button.click();
    fixture.detectChanges();
    expect(administration.requeue).toHaveBeenCalledOnce();
    expect(administration.requeue).toHaveBeenCalledWith('17');
    expect(button.textContent).toContain('Requeuing…');
    expect(element.textContent).not.toContain('Execution info JSON');

    requeueResult.next({
      id: '17',
      payloadId: 17,
      state: 'New',
      retryCount: 0,
      errorMessage: 'Projection failed.',
      stateMachineId: 'skills-state-machine',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      orderNumber: 7,
      eventName: 'SkillUpdatedV1',
      timestamp: '2026-08-22T12:00:00Z',
      executionInfoJson: '{\n  "eventName": "SkillUpdatedV1"\n}',
      eventDataJson: '{\n  "name": "Updated skill"\n}',
    });
    requeueResult.complete();
    fixture.detectChanges();

    expect(element.textContent).toContain('Ready to retry');
    expect(button.disabled).toBe(false);
  });

  it('expands and collapses both JSON documents from a payload row', () => {
    const element = fixture.nativeElement as HTMLElement;
    const row = element.querySelector('.payload-row') as HTMLTableRowElement;

    expect(row.getAttribute('aria-expanded')).toBe('false');
    expect(element.textContent).not.toContain('Execution info JSON');

    row.click();
    fixture.detectChanges();

    expect(row.getAttribute('aria-expanded')).toBe('true');
    expect(element.textContent).toContain('Execution info JSON');
    expect(element.textContent).toContain('Event data JSON');
    expect(element.textContent).toContain('SkillUpdatedV1');
    expect(element.textContent).toContain('Updated skill');

    row.click();
    fixture.detectChanges();

    expect(row.getAttribute('aria-expanded')).toBe('false');
    expect(element.textContent).not.toContain('Execution info JSON');
  });
});
