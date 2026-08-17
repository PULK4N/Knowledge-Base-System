import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PolicyService } from './policy.service';
import { PolicyScope } from './policy.models';

describe('PolicyService', () => {
  let service: PolicyService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PolicyService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads a nested topic policy route and maps policy IDs', async () => {
    const resultPromise = firstValueFrom(
      service.searchPolicies(
        { kind: 'topic', topicName: 'Web Design' },
        { page: 2, pageSize: 5, search: 'layout' },
      ),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url ===
          '/api/policies/topics/Web%20Design/policies' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '5' &&
        candidate.params.get('search') === 'layout',
    );

    request.flush({
      items: [
        {
          policyId: 'policy-1',
          title: 'Use responsive layouts',
          description: 'Support narrow and wide screens.',
        },
      ],
      page: 2,
      pageSize: 5,
      totalCount: 6,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    const result = await resultPromise;
    expect(result.items[0]).toEqual({
      id: 'policy-1',
      title: 'Use responsive layouts',
      description: 'Support narrow and wide screens.',
    });
  });

  it('loads project details including related topic names', async () => {
    const projectPromise = firstValueFrom(service.watchProject('project-1'));
    const request = http.expectOne('/api/policies/projects/project-1');

    request.flush({
      projectId: 'project-1',
      projectName: 'Knowledge Base System',
      projectDescription: 'Agent knowledge application.',
      repositoryPaths: ['/workspace/knowledge-base'],
      topicNames: ['Angular', 'Event sourcing'],
    });

    await expect(projectPromise).resolves.toEqual({
      id: 'project-1',
      name: 'Knowledge Base System',
      description: 'Agent knowledge application.',
      repositoryPaths: ['/workspace/knowledge-base'],
      topicNames: ['Angular', 'Event sourcing'],
    });
  });

  it.each<{
    scope: PolicyScope;
    path: string;
    identity: Readonly<Record<string, string>>;
  }>([
    {
      scope: { kind: 'general' },
      path: '/api/policies/general/update',
      identity: {},
    },
    {
      scope: { kind: 'topic', topicName: 'Angular' },
      path: '/api/policies/topics/policies/update',
      identity: { topicName: 'Angular' },
    },
    {
      scope: { kind: 'project', projectId: 'project-1' },
      path: '/api/policies/projects/policies/update',
      identity: { projectId: 'project-1' },
    },
  ])('updates a $scope.kind policy', async ({ scope, path, identity }) => {
    const policy = {
      policyId: 'policy-1',
      title: 'Use immutable state',
      description: 'Replace state instead of mutating it.',
    };
    const resultPromise = firstValueFrom(
      service.updatePolicy(scope, policy),
    );
    const request = http.expectOne(path);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ ...identity, ...policy });
    request.flush({ status: 'OK' });

    await expect(resultPromise).resolves.toEqual({ status: 'OK' });
  });

  it.each<{
    scope: PolicyScope;
    path: string;
    identity: Readonly<Record<string, string>>;
  }>([
    {
      scope: { kind: 'general' },
      path: '/api/policies/general/remove',
      identity: {},
    },
    {
      scope: { kind: 'topic', topicName: 'Angular' },
      path: '/api/policies/topics/policies/remove',
      identity: { topicName: 'Angular' },
    },
    {
      scope: { kind: 'project', projectId: 'project-1' },
      path: '/api/policies/projects/policies/remove',
      identity: { projectId: 'project-1' },
    },
  ])('removes a $scope.kind policy', async ({ scope, path, identity }) => {
    const resultPromise = firstValueFrom(
      service.removePolicy(scope, 'policy-1'),
    );
    const request = http.expectOne(path);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      ...identity,
      policyId: 'policy-1',
    });
    request.flush({ status: 'OK' });

    await expect(resultPromise).resolves.toEqual({ status: 'OK' });
  });
});
