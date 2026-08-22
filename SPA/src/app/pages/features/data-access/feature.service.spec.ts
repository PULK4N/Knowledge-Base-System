import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable, firstValueFrom } from 'rxjs';
import { Feature, FeatureDto } from './feature.models';
import { FeatureService } from './feature.service';

const feature: FeatureDto = {
  id: 'feature-1',
  isDeleted: false,
  projectId: 'project-1',
  name: 'Feature journal',
  summary: 'Trace implementation decisions.',
  status: 'Frontend implementation is in progress.',
  relatedSkillIds: ['skill-1'],
  records: [
    {
      id: 'record-1',
      userMessage: 'Why use a projection?',
      aiAnswer: 'To provide paged feature search.',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z',
    },
  ],
  plans: [
    {
      id: 'plan-1',
      title: 'Frontend plan',
      content: '# Build pages',
      contentType: 'Markdown',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  currentPlanId: 'plan-1',
};

describe('FeatureService', () => {
  let service: FeatureService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeatureService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches feature summaries and maps aggregate IDs', async () => {
    const resultPromise = firstValueFrom(
      service.search({ page: 2, pageSize: 6, search: ' journal ' }),
    );
    const request = http.expectOne(
      request =>
        request.url === '/api/features' &&
        request.params.get('page') === '2' &&
        request.params.get('pageSize') === '6' &&
        request.params.get('search') === 'journal',
    );

    expect(request.request.method).toBe('GET');
    request.flush({
      items: [
        {
          featureId: feature.id,
          projectId: feature.projectId,
          name: feature.name,
          summary: feature.summary,
          status: feature.status,
          currentPlanId: feature.currentPlanId,
          planCount: 1,
          recordCount: 1,
        },
      ],
      page: 2,
      pageSize: 6,
      totalCount: 7,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    const result = await resultPromise;
    expect(result.items[0].id).toBe(feature.id);
    expect(result.totalCount).toBe(7);
  });

  it('loads full feature state and derives summary counts', async () => {
    const resultPromise = firstValueFrom(service.watch(feature.id));
    const request = http.expectOne('/api/features/feature-1');

    expect(request.request.method).toBe('GET');
    request.flush(feature);

    await expect(resultPromise).resolves.toEqual({
      ...feature,
      planCount: 1,
      recordCount: 1,
    });
  });

  it('creates and refreshes a feature', async () => {
    const request = {
      projectId: feature.projectId,
      name: feature.name,
      summary: feature.summary,
      status: feature.status,
    };
    const resultPromise = firstValueFrom(service.create(request));
    const creation = http.expectOne('/api/features');

    expect(creation.request.method).toBe('POST');
    expect(creation.request.body).toEqual(request);
    creation.flush({ status: 'OK', featureId: feature.id });

    http.expectOne('/api/features/feature-1').flush(feature);
    expect((await resultPromise).id).toBe(feature.id);
  });

  it.each([
    {
      name: 'updates status',
      path: 'status',
      body: { status: 'Complete' },
      action: (api: FeatureService) => api.updateStatus(feature.id, 'Complete'),
    },
    {
      name: 'adds a skill',
      path: 'skills',
      body: { skillId: 'skill-2' },
      action: (api: FeatureService) => api.addSkill(feature.id, 'skill-2'),
    },
    {
      name: 'removes a skill',
      path: 'skills/remove',
      body: { skillId: 'skill-1' },
      action: (api: FeatureService) => api.removeSkill(feature.id, 'skill-1'),
    },
    {
      name: 'adds a record',
      path: 'records',
      body: { userMessage: 'Question', aiAnswer: 'Answer' },
      action: (api: FeatureService) =>
        api.addRecord(feature.id, { userMessage: 'Question', aiAnswer: 'Answer' }),
      result: { status: 'OK', recordId: 'record-2' },
    },
    {
      name: 'updates a record',
      path: 'records/update',
      body: { recordId: 'record-1', userMessage: 'Question', aiAnswer: 'Answer' },
      action: (api: FeatureService) =>
        api.updateRecord(feature.id, {
          recordId: 'record-1',
          userMessage: 'Question',
          aiAnswer: 'Answer',
        }),
    },
    {
      name: 'removes a record',
      path: 'records/remove',
      body: { recordId: 'record-1' },
      action: (api: FeatureService) => api.removeRecord(feature.id, 'record-1'),
    },
    {
      name: 'adds a plan',
      path: 'plans',
      body: { title: 'Plan', content: '# Plan', contentType: 'Markdown' },
      action: (api: FeatureService) =>
        api.addPlan(feature.id, {
          title: 'Plan',
          content: '# Plan',
          contentType: 'Markdown',
        }),
      result: { status: 'OK', planId: 'plan-2' },
    },
    {
      name: 'updates the current plan',
      path: 'plans/current',
      body: { title: 'Plan', content: '<p>Plan</p>', contentType: 'Html' },
      action: (api: FeatureService) =>
        api.updateCurrentPlan(feature.id, {
          title: 'Plan',
          content: '<p>Plan</p>',
          contentType: 'Html',
        }),
    },
    {
      name: 'changes the current plan',
      path: 'plans/current/change',
      body: { planId: 'plan-1' },
      action: (api: FeatureService) =>
        api.changeCurrentPlan(feature.id, 'plan-1'),
    },
    {
      name: 'removes a plan',
      path: 'plans/remove',
      body: { planId: 'plan-1' },
      action: (api: FeatureService) => api.removePlan(feature.id, 'plan-1'),
    },
  ])('$name and refreshes feature state', async testCase => {
    const resultPromise = firstValueFrom(
      testCase.action(service) as Observable<Feature>,
    );
    const mutation = http.expectOne(`/api/features/feature-1/${testCase.path}`);

    expect(mutation.request.method).toBe('POST');
    expect(mutation.request.body).toEqual(testCase.body);
    mutation.flush(testCase.result ?? { status: 'OK' });
    http.expectOne('/api/features/feature-1').flush(feature);

    expect((await resultPromise).id).toBe(feature.id);
  });

  it('removes a feature through a POST action', async () => {
    const resultPromise = firstValueFrom(service.remove(feature.id));
    const request = http.expectOne('/api/features/feature-1/remove');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ status: 'OK' });

    await expect(resultPromise).resolves.toEqual({ status: 'OK' });
  });
});
