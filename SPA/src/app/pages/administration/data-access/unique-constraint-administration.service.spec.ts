import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { UniqueConstraintRebuildResult } from './unique-constraint-administration.models';
import { UniqueConstraintAdministrationService } from './unique-constraint-administration.service';

describe('UniqueConstraintAdministrationService', () => {
  let service: UniqueConstraintAdministrationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UniqueConstraintAdministrationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads state machine IDs', async () => {
    const resultPromise = firstValueFrom(service.listStateMachines());

    http
      .expectOne('/api/administration/unique-constraints/state-machines')
      .flush(['features-state-machine', 'skills-state-machine']);

    await expect(resultPromise).resolves.toEqual([
      'features-state-machine',
      'skills-state-machine',
    ]);
  });

  it('rebuilds the constraints of an encoded state machine', async () => {
    const result: UniqueConstraintRebuildResult = {
      status: 'Completed',
      processedAggregateCount: 4,
      constraintCount: 3,
      conflicts: [],
    };
    const resultPromise = firstValueFrom(service.rebuild('skill/state machine'));
    const request = http.expectOne(
      '/api/administration/unique-constraints/skill%2Fstate%20machine/rebuild',
    );

    expect(request.request.method).toBe('POST');
    request.flush(result);

    await expect(resultPromise).resolves.toEqual(result);
  });
});
