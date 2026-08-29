import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import { BehaviorSubject, Subject, of } from 'rxjs';
import { OutboxPayload } from '../data-access/outbox-administration.models';
import { OutboxAdministrationService } from '../data-access/outbox-administration.service';
import { OutboxAdministrationPage } from './outbox-administration.page';

const PAYLOAD: OutboxPayload = {
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
  executionInfoJson: '{\n  "eventName": "SkillUpdatedV1"\n}',
  eventDataJson: '{\n  "name": "Updated skill"\n}',
};

describe('OutboxAdministrationPage', () => {
  let fixture: ComponentFixture<OutboxAdministrationPage>;
  let params: BehaviorSubject<ParamMap>;
  let requeueResult: Subject<OutboxPayload>;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let administration: {
    search: ReturnType<typeof vi.fn>;
    requeue: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    params = new BehaviorSubject(convertToParamMap({}));
    requeueResult = new Subject<OutboxPayload>();
    router = { navigate: vi.fn() };
    administration = {
      search: vi.fn(() =>
        of({
          items: [PAYLOAD],
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
          provide: ActivatedRoute,
          useValue: { queryParamMap: params.asObservable() },
        },
        { provide: Router, useValue: router },
        {
          provide: OutboxAdministrationService,
          useValue: administration,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OutboxAdministrationPage);
    fixture.detectChanges();
  });

  it('reads list state from the URL and requeues without duplicate submits', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('SkillUpdatedV1');
    expect(element.textContent).toContain('Projection failed.');
    expect(administration.search).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 10,
      search: '',
      onlyIncomplete: false,
      state: '',
      aggregateId: '',
      sortBy: 'Id',
      sortDirection: 'Descending',
    });

    params.next(
      convertToParamMap({
        state: 'Error',
        sortBy: 'RetryCount',
        sortDirection: 'Ascending',
      }),
    );
    fixture.detectChanges();
    expect(administration.search).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 10,
      search: '',
      onlyIncomplete: false,
      state: 'Error',
      aggregateId: '',
      sortBy: 'RetryCount',
      sortDirection: 'Ascending',
    });

    const sortSelect = element.querySelector(
      '.list-controls select',
    ) as HTMLSelectElement;
    const stateSelect = Array.from(
      element.querySelectorAll('.list-filters select'),
    )[1] as HTMLSelectElement;
    expect(sortSelect.value).toBe('RetryCount');
    expect(stateSelect.value).toBe('Error');

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

    requeueResult.next({ ...PAYLOAD, state: 'New', retryCount: 0 });
    requeueResult.complete();
    fixture.detectChanges();

    expect(element.textContent).toContain('Ready to retry');
    expect(button.disabled).toBe(false);
  });

  it('publishes filter and sort changes as query parameters', () => {
    const element = fixture.nativeElement as HTMLElement;
    const [completionFilter, stateFilter] = Array.from(
      element.querySelectorAll('.list-filters select'),
    ) as HTMLSelectElement[];

    completionFilter.value = 'true';
    completionFilter.dispatchEvent(new Event('change'));
    expect(router.navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({
          page: null,
          onlyIncomplete: 'true',
          state: null,
        }),
      }),
    );

    stateFilter.value = 'Sent';
    stateFilter.dispatchEvent(new Event('change'));
    expect(router.navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ state: 'Sent' }),
      }),
    );

    const sortSelect = element.querySelector(
      '.list-controls select',
    ) as HTMLSelectElement;
    sortSelect.value = 'RetryCount';
    sortSelect.dispatchEvent(new Event('change'));
    expect(router.navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ sortBy: 'RetryCount' }),
      }),
    );
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
