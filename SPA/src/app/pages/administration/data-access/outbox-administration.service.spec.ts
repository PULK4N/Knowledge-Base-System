import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { OutboxAdministrationService } from './outbox-administration.service';

describe('OutboxAdministrationService', () => {
  let service: OutboxAdministrationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OutboxAdministrationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads a filtered outbox page and maps numeric payload IDs', async () => {
    const resultPromise = firstValueFrom(
      service.search({ page: 2, pageSize: 10, onlyIncomplete: true }),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url === '/api/administration/outbox' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '10' &&
        candidate.params.get('onlyIncomplete') === 'true',
    );

    request.flush({
      items: [
        {
          id: 17,
          state: 'Error',
          retryCount: 3,
          errorMessage: 'Projection failed.',
          stateMachineId: 'skills-state-machine',
          aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          orderNumber: 7,
          eventName: 'SkillUpdatedV1',
          timestamp: '2026-08-22T12:00:00Z',
          executionInfoJson: '{"eventName":"SkillUpdatedV1"}',
          eventDataJson: '{"name":"Updated skill"}',
        },
      ],
      page: 2,
      pageSize: 10,
      totalCount: 11,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    const result = await resultPromise;
    expect(result.items[0].id).toBe('17');
    expect(result.items[0].payloadId).toBe(17);
    expect(result.items[0].executionInfoJson).toBe(
      '{\n  "eventName": "SkillUpdatedV1"\n}',
    );
  });

  it('requeues a payload and returns the reset entity', async () => {
    const resultPromise = firstValueFrom(service.requeue('17'));
    const request = http.expectOne(
      '/api/administration/outbox/17/requeue',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({
      id: 17,
      state: 'New',
      retryCount: 0,
      errorMessage: 'Projection failed.',
      stateMachineId: 'skills-state-machine',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      orderNumber: 7,
      eventName: 'SkillUpdatedV1',
      timestamp: '2026-08-22T12:00:00Z',
      executionInfoJson: '{"eventName":"SkillUpdatedV1"}',
      eventDataJson: '{"name":"Updated skill"}',
    });

    await expect(resultPromise).resolves.toMatchObject({
      id: '17',
      payloadId: 17,
      state: 'New',
      retryCount: 0,
    });
  });
});
