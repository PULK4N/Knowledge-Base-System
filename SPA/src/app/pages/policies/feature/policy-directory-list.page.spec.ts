import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { BehaviorSubject, of } from 'rxjs';
import { PolicyTopicSearchResult } from '../data-access/policy.models';
import { PolicyService } from '../data-access/policy.service';
import { PolicyDirectoryListPage } from './policy-directory-list.page';

describe('PolicyDirectoryListPage topic deletion', () => {
  it('requires confirmation and removes the selected topic', async () => {
    const topics$ = new BehaviorSubject<PolicyTopicSearchResult>({
      items: [
        {
          id: 'Angular',
          name: 'Angular',
          description: 'Angular application guidance.',
          policyCount: 2,
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });
    const policies = {
      searchTopics: vi.fn(() => topics$),
      removeTopic: vi.fn((topicName: string) => {
        topics$.next({
          ...topics$.value,
          items: [],
          totalCount: 0,
          totalPages: 0,
        });
        return of({ status: 'OK' });
      }),
    };

    await TestBed.configureTestingModule({
      imports: [PolicyDirectoryListPage],
      providers: [
        provideRouter([
          {
            path: 'policies/topics',
            component: PolicyDirectoryListPage,
            data: { directoryKind: 'topics' },
          },
        ]),
        { provide: PolicyService, useValue: policies },
      ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create('/policies/topics');
    const element = harness.routeNativeElement as HTMLElement;
    const deleteButton = element.querySelector(
      '.directory-actions .danger-link',
    ) as HTMLButtonElement;

    deleteButton.click();
    harness.detectChanges();

    expect(element.textContent).toContain('Delete this topic?');
    expect(policies.removeTopic).not.toHaveBeenCalled();

    const confirmButton = element.querySelector(
      '.directory-actions .danger-action',
    ) as HTMLButtonElement;
    confirmButton.click();
    harness.detectChanges();

    expect(policies.removeTopic).toHaveBeenCalledWith('Angular');
    expect(element.querySelector('.directory-row')).toBeNull();
  });
});
