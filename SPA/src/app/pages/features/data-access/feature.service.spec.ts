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
      type: 'conversation',
      title: 'Why use a projection?',
      content: 'To provide paged feature search.',
      sourceType: null,
      sourceReference: '',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z',
    },
    {
      id: 'discovery-1',
      type: 'research',
      title: 'YAML configuration',
      content: 'Feature transitions are configured in YAML.',
      sourceType: 'Code',
      sourceReference: 'StateMachines/features.yaml',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  reviewNotes: [
    {
      id: 'review-note-1',
      title: 'Replay reloads state',
      content: 'State replay is intentional.',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  plans: [
    {
      id: 'plan-1',
      title: 'Frontend plan',
      content: '# Build pages',
      contentType: 'Markdown',
      additionalInfo: 'Production implementation.',
      status: 'InProgress',
      branches: [{ name: 'feature/plans', description: '', subBranches: [], sources: [] }],
      previousPlanId: null,
      recordIds: ['discovery-1'],
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  branch: null,
  branchGraph: [],
  memoryHistory: [],
};

const planContent = {
  title: 'Plan',
  content: '<p>Plan</p>',
  contentType: 'Html',
  additionalInfo: 'Research only.',
  status: 'NotStarted',
} as const;

const addPlanRequest = {
  ...planContent,
  branchName: 'feature/plans',
  branchDescription: 'Plan rework',
  previousPlanId: 'plan-1',
  recordIds: ['record-1'],
} as const;

const recordContent = {
  type: 'research',
  title: 'YAML configuration',
  content: 'Feature transitions are configured in YAML.',
  sourceType: 'Code',
  sourceReference: 'StateMachines/features.yaml',
} as const;

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
      service.search({
        page: 2,
        pageSize: 6,
        search: ' journal ',
        projectId: 'project-1',
        sortBy: 'RecordCount',
        sortDirection: 'Descending',
      }),
    );
    const request = http.expectOne(
      request =>
        request.url === '/api/features' &&
        request.params.get('page') === '2' &&
        request.params.get('pageSize') === '6' &&
        request.params.get('search') === 'journal' &&
        request.params.get('projectId') === 'project-1' &&
        request.params.get('sortBy') === 'RecordCount' &&
        request.params.get('sortDirection') === 'Descending',
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
      recordCount: 2,
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
      name: 'updates summary',
      path: 'summary',
      body: { summary: 'Updated feature summary.' },
      action: (api: FeatureService) =>
        api.updateSummary(feature.id, 'Updated feature summary.'),
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
      name: 'adds a record linked to plans',
      path: 'records',
      body: { ...recordContent, planIds: ['plan-1'] },
      action: (api: FeatureService) =>
        api.addRecord(feature.id, { ...recordContent, planIds: ['plan-1'] }),
      result: { status: 'OK', recordId: 'record-2' },
    },
    {
      name: 'updates a record',
      path: 'records/update',
      body: { ...recordContent, recordId: 'record-1' },
      action: (api: FeatureService) =>
        api.updateRecord(feature.id, { ...recordContent, recordId: 'record-1' }),
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
      body: addPlanRequest,
      action: (api: FeatureService) => api.addPlan(feature.id, addPlanRequest),
      result: { status: 'OK', planId: 'plan-2' },
    },
    {
      name: 'updates a plan by ID',
      path: 'plans/plan-1/update',
      body: planContent,
      action: (api: FeatureService) =>
        api.updatePlan(feature.id, 'plan-1', planContent),
    },
    {
      name: 'changes a plan status',
      path: 'plans/plan-1/status',
      body: { status: 'Delayed' },
      action: (api: FeatureService) =>
        api.changePlanStatus(feature.id, 'plan-1', 'Delayed'),
    },
    {
      name: 'sets the feature branch',
      path: 'branch',
      body: { branchName: 'develop', description: 'Main line' },
      action: (api: FeatureService) =>
        api.setBranch(feature.id, { branchName: 'develop', description: 'Main line' }),
    },
    {
      name: 'clears a plan previous plan',
      path: 'plans/plan-1/previous-plan',
      body: { previousPlanId: null },
      action: (api: FeatureService) =>
        api.setPlanPreviousPlan(feature.id, 'plan-1', null),
    },
    {
      name: 'removes a plan',
      path: 'plans/plan-1/remove',
      body: {},
      action: (api: FeatureService) => api.removePlan(feature.id, 'plan-1'),
    },
    {
      name: 'adds a plan sub-branch',
      path: 'plans/plan-1/branches',
      body: {
        branchName: 'feature/plans-api',
        description: 'Backend',
        parentBranchName: 'feature/plans',
      },
      action: (api: FeatureService) =>
        api.addPlanBranch(feature.id, 'plan-1', {
          branchName: 'feature/plans-api',
          description: 'Backend',
          parentBranchName: 'feature/plans',
        }),
    },
    {
      name: 'renames a plan branch',
      path: 'plans/plan-1/branches/update',
      body: {
        branchName: 'feature/plans-api',
        newBranchName: 'feature/plans-backend',
        description: 'Backend',
      },
      action: (api: FeatureService) =>
        api.updatePlanBranch(feature.id, 'plan-1', {
          branchName: 'feature/plans-api',
          newBranchName: 'feature/plans-backend',
          description: 'Backend',
        }),
    },
    {
      name: 'removes a plan branch',
      path: 'plans/plan-1/branches/remove',
      body: { branchName: 'feature/plans-api' },
      action: (api: FeatureService) =>
        api.removePlanBranch(feature.id, 'plan-1', 'feature/plans-api'),
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
