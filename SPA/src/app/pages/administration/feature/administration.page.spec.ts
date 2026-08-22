import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { OutboxAdministrationService } from '../data-access/outbox-administration.service';
import { ProjectionAdministrationService } from '../data-access/projection-administration.service';
import { AdministrationPage } from './administration.page';
import { OutboxAdministrationPage } from './outbox-administration.page';
import { ProjectionAdministrationPage } from './projection-administration.page';

describe('AdministrationPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'administration',
            component: AdministrationPage,
            children: [
              {
                path: '',
                pathMatch: 'full',
                redirectTo: 'projections',
              },
              {
                path: 'projections',
                component: ProjectionAdministrationPage,
              },
              {
                path: 'outbox',
                component: OutboxAdministrationPage,
              },
            ],
          },
        ]),
        {
          provide: ProjectionAdministrationService,
          useValue: {
            list: vi.fn(() => of([])),
            execute: vi.fn(),
          },
        },
        {
          provide: OutboxAdministrationService,
          useValue: {
            search: vi.fn(() =>
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
            requeue: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('renders the selected administration tab from the URL', async () => {
    const harness = await RouterTestingHarness.create(
      '/administration/outbox',
    );
    const router = TestBed.inject(Router);
    const element = harness.routeNativeElement as HTMLElement;
    const outboxTab = element.querySelector('#outbox-tab') as HTMLAnchorElement;

    expect(router.url).toBe('/administration/outbox');
    expect(outboxTab.getAttribute('aria-selected')).toBe('true');
    expect(element.textContent).toContain('Outbox payloads');
    expect(element.textContent).toContain('The outbox is empty');

    await harness.navigateByUrl('/administration/projections');

    const projectionsTab = element.querySelector(
      '#projections-tab',
    ) as HTMLAnchorElement;
    expect(router.url).toBe('/administration/projections');
    expect(projectionsTab.getAttribute('aria-selected')).toBe('true');
    expect(element.textContent).toContain('Projection groups');
  });
});
