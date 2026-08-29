import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { PoliciesPage } from './policies.page';

@Component({ template: '<p>Policy scope content</p>' })
class PolicyScopeTestPage {}

describe('PoliciesPage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'policies',
            component: PoliciesPage,
            children: [
              { path: 'general', component: PolicyScopeTestPage },
              { path: 'topics', component: PolicyScopeTestPage },
              { path: 'projects', component: PolicyScopeTestPage },
            ],
          },
        ]),
      ],
    });
  });

  it('keeps one policy workspace and selects its scope from the URL', async () => {
    const harness = await RouterTestingHarness.create('/policies/general');
    harness.detectChanges();
    const router = TestBed.inject(Router);
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelectorAll('[role="tab"]')).toHaveLength(3);
    expect(
      element
        .querySelector('#general-policies-tab')
        ?.getAttribute('aria-selected'),
    ).toBe('true');

    await harness.navigateByUrl('/policies/topics');
    harness.detectChanges();

    expect(router.url).toBe('/policies/topics');
    expect(
      element
        .querySelector('#topic-policies-tab')
        ?.getAttribute('aria-selected'),
    ).toBe('true');

    await harness.navigateByUrl('/policies/projects');
    harness.detectChanges();

    expect(router.url).toBe('/policies/projects');
    expect(
      element
        .querySelector('#project-policies-tab')
        ?.getAttribute('aria-selected'),
    ).toBe('true');
  });
});
