import { TestBed } from '@angular/core/testing';
import { convertToParamMap } from '@angular/router';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { BehaviorSubject, of } from 'rxjs';
import { PolicyProjectDetails } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PolicyListPage, policyScopeFromRoute } from './policy-list.page';

describe('policyScopeFromRoute', () => {
  it('creates the general policy scope without route parameters', () => {
    expect(policyScopeFromRoute('general', convertToParamMap({}))).toEqual({
      kind: 'general',
    });
  });

  it('creates scoped policy contexts from their route parameters', () => {
    expect(
      policyScopeFromRoute(
        'topic',
        convertToParamMap({ topicName: 'Web Design' }),
      ),
    ).toEqual({ kind: 'topic', topicName: 'Web Design' });
    expect(
      policyScopeFromRoute(
        'agentFamily',
        convertToParamMap({ agentFamilyName: 'claude' }),
      ),
    ).toEqual({ kind: 'agentFamily', agentFamilyName: 'claude' });

    expect(
      policyScopeFromRoute(
        'project',
        convertToParamMap({ projectId: 'project-1' }),
      ),
    ).toEqual({ kind: 'project', projectId: 'project-1' });
  });

  it('rejects missing or unknown route contexts', () => {
    expect(policyScopeFromRoute('topic', convertToParamMap({}))).toBeNull();
    expect(
      policyScopeFromRoute('agentFamily', convertToParamMap({})),
    ).toBeNull();
    expect(policyScopeFromRoute('unknown', convertToParamMap({}))).toBeNull();
  });
});

describe('PolicyListPage project topics', () => {
  it('requires confirmation and disconnects a topic from the project', async () => {
    const project$ = new BehaviorSubject<PolicyProjectDetails>({
      id: 'project-1',
      name: 'Knowledge Base System',
      description: 'Agent knowledge application.',
      repositoryPaths: ['/workspace/knowledge-base'],
      topicNames: ['Angular'],
    });
    const policies = {
      searchPolicies: vi.fn(() =>
        of({
          items: [],
          page: 1,
          pageSize: 10,
          totalCount: 0,
          totalPages: 0,
          hasPreviousPage: false,
          hasNextPage: false,
        }),
      ),
      watchProject: vi.fn(() => project$),
      removeProjectTopic: vi.fn((projectId: string, topicName: string) => {
        const updatedProject = {
          ...project$.value,
          topicNames: project$.value.topicNames.filter(
            candidate => candidate !== topicName,
          ),
        };
        project$.next(updatedProject);
        return of(updatedProject);
      }),
    };

    await TestBed.configureTestingModule({
      imports: [PolicyListPage],
      providers: [
        provideRouter([
          {
            path: 'policies/projects/:projectId',
            component: PolicyListPage,
            data: { policyScope: 'project' },
          },
        ]),
        { provide: PolicyService, useValue: policies },
      ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create(
      '/policies/projects/project-1',
    );
    const element = harness.routeNativeElement as HTMLElement;
    const removeButton = element.querySelector(
      '.topic-remove-button',
    ) as HTMLButtonElement;

    removeButton.click();
    harness.detectChanges();

    expect(element.textContent).toContain('Disconnect?');
    expect(policies.removeProjectTopic).not.toHaveBeenCalled();

    const confirmButton = element.querySelector(
      '.topic-removal-confirmation .danger-action',
    ) as HTMLButtonElement;
    confirmButton.click();
    harness.detectChanges();

    expect(policies.removeProjectTopic).toHaveBeenCalledWith(
      'project-1',
      'Angular',
    );
    expect(element.querySelector('.topic-connection')).toBeNull();
  });
});
