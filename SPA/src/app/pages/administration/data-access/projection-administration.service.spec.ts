import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ProjectionAdministrationService } from './projection-administration.service';

describe('ProjectionAdministrationService', () => {
  let service: ProjectionAdministrationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProjectionAdministrationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads projection groups', async () => {
    const resultPromise = firstValueFrom(service.list());
    const request = http.expectOne('/api/administration/projections');

    request.flush([
      {
        stateMachineId: 'skill-state-machine',
        projectionNames: ['SkillSummaryProjector', 'SkillSearchProjector'],
      },
    ]);

    await expect(resultPromise).resolves.toEqual([
      {
        stateMachineId: 'skill-state-machine',
        projectionNames: ['SkillSummaryProjector', 'SkillSearchProjector'],
      },
    ]);
  });

  it('queues the latest aggregate events for a state machine', async () => {
    const resultPromise = firstValueFrom(
      service.execute('skill/state machine'),
    );
    const request = http.expectOne(
      '/api/administration/projections/skill%2Fstate%20machine/execute',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ status: 'Queued', queuedAggregateCount: 3 });

    await expect(resultPromise).resolves.toEqual({
      status: 'Queued',
      queuedAggregateCount: 3,
    });
  });

  it('runs one projection for the requested scope', async () => {
    const body = {
      projectionName: 'SkillSearchProjector',
      aggregateId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    };
    const resultPromise = firstValueFrom(service.run(body));
    const request = http.expectOne(
      '/api/administration/projections/run',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ status: 'Completed', processedAggregateCount: 1 });

    await expect(resultPromise).resolves.toEqual({
      status: 'Completed',
      processedAggregateCount: 1,
    });
  });
});
