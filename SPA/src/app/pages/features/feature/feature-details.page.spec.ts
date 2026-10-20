import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { provideKnowledgeMarkdown } from '../../../shared/markdown/markdown.providers';
import { SkillService } from '../../skills/data-access/skill.service';
import { Feature } from '../data-access/feature.models';
import { FeatureService } from '../data-access/feature.service';
import { FeatureDetailsPage } from './feature-details.page';

const feature: Feature = {
  id: 'feature-1',
  isDeleted: false,
  projectId: 'project-1',
  name: 'Feature journal',
  summary: 'Trace implementation decisions.',
  status: 'Frontend implementation is in progress.',
  planCount: 0,
  recordCount: 2,
  relatedSkillIds: [],
  records: [
    {
      id: 'record-1',
      type: 'conversation',
      title: 'Can this be Markdown?',
      content: '## User\n\nCan this be **Markdown**?\n\n## AI\n\nYes.',
      sourceType: null,
      sourceReference: '',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T10:30:00Z',
    },
    {
      id: 'discovery-1',
      type: 'research',
      title: 'YAML configuration',
      content: '## Finding\n\nFeature transitions are configured in **YAML**.',
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
      content: '## Decision\n\nState replay is **intentional**.',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  plans: [],
  branch: null,
  branchGraph: [],
  memoryHistory: [
    {
      eventName: 'FeatureAddedV2',
      timestamp: '2026-08-20T09:00:00Z',
      memoryId: 'memory-1',
      isUserOriginated: false,
    },
    {
      eventName: 'FeatureSummaryUpdatedV2',
      timestamp: '2026-08-21T09:00:00Z',
      memoryId: 'user-memory',
      isUserOriginated: true,
    },
  ],
};

const featureWithPlans: Feature = {
  ...feature,
  planCount: 2,
  plans: [
    {
      id: 'plan-current',
      title: 'Current plan',
      content: '# Current',
      contentType: 'Markdown',
      additionalInfo: 'Production implementation.',
      status: 'InProgress',
      branches: [
        {
          name: 'feature/plans',
          description: 'Plan rework',
          subBranches: [
            { name: 'feature/plans-api', description: '', subBranches: [], sources: [] },
          ],
          sources: [],
        },
      ],
      previousPlanId: null,
      recordIds: ['discovery-1'],
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z',
    },
    {
      id: 'plan-previous',
      title: 'Previous plan',
      content: '# Previous',
      contentType: 'Markdown',
      additionalInfo: '',
      status: 'Completed',
      branches: [],
      previousPlanId: null,
      recordIds: [],
      createdAt: '2026-08-18T10:00:00Z',
      updatedAt: '2026-08-19T10:00:00Z',
    },
  ],
};

describe('FeatureDetailsPage', () => {
  let harness: RouterTestingHarness;
  let features: {
    watch: ReturnType<typeof vi.fn>;
    updateSummary: ReturnType<typeof vi.fn>;
    addRecord: ReturnType<typeof vi.fn>;
    updateRecord: ReturnType<typeof vi.fn>;
    removeRecord: ReturnType<typeof vi.fn>;
    addReviewNote: ReturnType<typeof vi.fn>;
    updateReviewNote: ReturnType<typeof vi.fn>;
    removeReviewNote: ReturnType<typeof vi.fn>;
    addPlan: ReturnType<typeof vi.fn>;
    removePlan: ReturnType<typeof vi.fn>;
    setCompositeBranch: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    Object.assign(globalThis, {
      mermaid: {
        initialize: vi.fn(),
        run: vi.fn(),
      },
    });
    features = {
      watch: vi.fn(() => of(feature)),
      updateSummary: vi.fn(() => of(feature)),
      addRecord: vi.fn(() => of(feature)),
      updateRecord: vi.fn(() => of(feature)),
      removeRecord: vi.fn(() => of(feature)),
      addReviewNote: vi.fn(() => of(feature)),
      updateReviewNote: vi.fn(() => of(feature)),
      removeReviewNote: vi.fn(() => of(feature)),
      addPlan: vi.fn(() => of(feature)),
      removePlan: vi.fn(() => of(feature)),
      setCompositeBranch: vi.fn(() => of(feature)),
    };

    await TestBed.configureTestingModule({
      imports: [FeatureDetailsPage],
      providers: [
        provideRouter([
          {
            path: 'features/:featureId',
            component: FeatureDetailsPage,
          },
        ]),
        ...provideKnowledgeMarkdown(),
        { provide: FeatureService, useValue: features },
        {
          provide: SkillService,
          useValue: {
            search: vi.fn(() =>
              of({
                items: [{ id: 'skill-1', name: 'Angular code writer' }],
                page: 1,
                pageSize: 100,
                totalCount: 1,
                totalPages: 1,
                hasPreviousPage: false,
                hasNextPage: false,
              }),
            ),
          },
        },
      ],
    }).compileComponents();

    harness = await RouterTestingHarness.create(
      '/features/feature-1?tab=records',
    );
  });

  it('shows the main branch above the composite branch only on the overview and retains failed edits', async () => {
    const shown: Feature = {
      ...feature,
      branch: { name: 'feature/main', description: '' },
      compositeBranchName: 'feature/testing',
    };
    features.watch.mockReturnValue(of(shown));
    features.setCompositeBranch.mockReturnValueOnce(throwError(() => new Error('Save failed')));
    await harness.navigateByUrl('/features/feature-branches?tab=overview', FeatureDetailsPage);
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;
    const settings = element.querySelector('app-feature-branch-settings')!;
    expect(settings.classList.contains('compact')).toBe(true);
    expect(settings.compareDocumentPosition(element.querySelector('.feature-overview')!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect([...settings.querySelectorAll('code')].map(item => item.textContent)).toEqual([
      'feature/main', 'feature/testing',
    ]);
    const composite = settings.querySelector('[data-branch-role="composite"]')!;
    composite.querySelector<HTMLButtonElement>('button')!.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    setControlValue(element, '[name="compositeBranchName"]', ' feature/all-testing ');
    composite.querySelector('form')!.dispatchEvent(new Event('submit'));
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(features.setCompositeBranch).toHaveBeenCalledWith(feature.id, { branchName: 'feature/all-testing' });
    expect(element.querySelector<HTMLInputElement>('[name="compositeBranchName"]')?.value).toBe(' feature/all-testing ');
    expect(element.querySelector('[role="alert"]')).not.toBeNull();

    await harness.navigateByUrl('/features/feature-branches?tab=plans', FeatureDetailsPage);
    harness.detectChanges();
    expect(element.querySelector('app-feature-branch-settings')).toBeNull();
    expect(element.textContent).not.toContain('feature/testing');
  });

  it('updates the feature summary from the overview', async () => {
    await harness.navigateByUrl(
      '/features/feature-1?tab=overview',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    const summaryBlock = element.querySelector('.summary-block') as HTMLElement;
    await vi.waitFor(() => {
      harness.detectChanges();
      expect(
        summaryBlock.querySelector('.overview-document')?.textContent,
      ).toContain('Trace implementation decisions.');
    });
    expect(summaryBlock.querySelector('textarea')).toBeNull();

    summaryBlock
      .querySelector<HTMLButtonElement>('.overview-field-heading button')
      ?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(
      element,
      '[name="summary"]',
      ' Updated implementation summary. ',
    );

    const form = summaryBlock.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.updateSummary).toHaveBeenCalledWith(
      feature.id,
      'Updated implementation summary.',
    );
    expect(summaryBlock.querySelector('textarea')).toBeNull();
    expect(summaryBlock.querySelector('.overview-document')).not.toBeNull();
  });

  it('renders the overview as a Markdown document without a toggle', async () => {
    features.watch.mockReturnValue(
      of({ ...feature, summary: '## Purpose\n\nA **short** overview.' }),
    );
    await harness.navigateByUrl(
      '/features/feature-2?tab=overview',
      FeatureDetailsPage,
    );
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    await vi.waitFor(() => {
      harness.detectChanges();
      const document = element.querySelector(
        '.summary-block .overview-document',
      ) as HTMLElement;

      expect(document.querySelector('h2')?.textContent).toBe('Purpose');
      expect(document.querySelector('strong')?.textContent).toBe('short');
    });
    expect(element.querySelector('.summary-block .text-toggle')).toBeNull();
    expect(element.querySelector('.status-block .text-toggle')).toBeNull();
  });

  it('shows the whole overview and no plan on the overview tab', async () => {
    features.watch.mockReturnValue(
      of({ ...featureWithPlans, summary: 'Overview line.\n'.repeat(12) }),
    );
    await harness.navigateByUrl(
      '/features/feature-overview?tab=overview',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelector('.summary-block .overview-document')).not.toBeNull();
    expect(element.querySelector('.summary-block .clamped')).toBeNull();
    expect(element.querySelector('.summary-block .text-toggle')).toBeNull();
    expect(element.querySelector('.current-plan')).toBeNull();
    expect(element.textContent).not.toContain('Current plan');
  });

  it('lists the newest change first and links chat changes to their memory', async () => {
    await harness.navigateByUrl(
      '/features/feature-history?tab=history',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    const items = Array.from(element.querySelectorAll('.history-item'));

    expect(items.map(item => item.querySelector('h3')?.textContent)).toEqual([
      'Summary updated',
      'Feature added',
    ]);
    expect(items[0].querySelector('.history-source')?.textContent).toContain(
      'Web app',
    );
    expect(
      items[1].querySelector('a.history-source')?.getAttribute('href'),
    ).toBe('/memories/memory-1');
  });

  it('shows related skills by name and falls back to a short ID', async () => {
    features.watch.mockReturnValue(
      of({ ...feature, relatedSkillIds: ['skill-1', 'skill-unknown'] }),
    );
    await harness.navigateByUrl(
      '/features/feature-skills?tab=overview',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    const names = Array.from(
      element.querySelectorAll('.skill-link-item strong'),
    ).map(item => item.textContent?.trim());

    expect(names).toEqual(['Angular code writer', 'skill-unknown']);
  });

  it('offers Show more only when the clamped status hides text', async () => {
    features.watch.mockReturnValue(
      of({ ...feature, status: 'Status line.\n'.repeat(12) }),
    );
    await harness.navigateByUrl(
      '/features/feature-3?tab=overview',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    const statusText = element.querySelector(
      '.status-block .overview-field-text',
    ) as HTMLElement;

    expect(element.querySelector('.status-block .text-toggle')).toBeNull();

    Object.defineProperty(statusText, 'scrollHeight', { value: 240 });
    Object.defineProperty(statusText, 'clientHeight', { value: 120 });
    TestBed.inject(ApplicationRef).tick();
    harness.detectChanges();

    const toggle = element.querySelector(
      '.status-block .text-toggle',
    ) as HTMLButtonElement;

    expect(statusText.classList).toContain('clamped');
    expect(toggle.textContent?.trim()).toBe('Show more');

    toggle.click();
    harness.detectChanges();

    expect(statusText.classList).not.toContain('clamped');
    expect(
      element.querySelector('.status-block .text-toggle')?.textContent?.trim(),
    ).toBe('Show less');
  });

  it('renders records as Markdown with their type and source', async () => {
    const element = harness.routeNativeElement as HTMLElement;
    expect(element.querySelector('.section-heading h2')?.textContent).toBe('Records');
    expect(element.querySelector('form.research-discovery-editor')).toBeNull();

    await vi.waitFor(() => {
      harness.detectChanges();
      const items = element.querySelectorAll<HTMLElement>('.research-discovery-item');

      expect(items).toHaveLength(2);
      expect(items[0].querySelector('.record-type')?.textContent?.trim()).toBe('research');
      expect(items[0].querySelector('.markdown-document h2')?.textContent).toBe('Finding');
      expect(items[0].textContent).toContain('StateMachines/features.yaml');
      expect(items[1].querySelector('.record-type')?.textContent?.trim()).toBe('conversation');
      expect(items[1].querySelector('.source-type')).toBeNull();
      expect(items[1].querySelector('.markdown-document strong')?.textContent).toBe('Markdown');
    });
  });

  it('submits a new record with a lowercase type and linked plans', async () => {
    const element = await openRecordsTab(harness, features, featureWithPlans);

    element
      .querySelector<HTMLButtonElement>('.section-actions .add-toggle')
      ?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(element, '[name="recordType"]', ' Decision ');
    setControlValue(element, '[name="recordTitle"]', 'Documentation behavior ');
    setControlValue(element, '[name="recordContent"]', 'Documentation confirms the behavior. ');
    setControlValue(element, '[name="recordSourceType"]', 'Web', 'change');
    harness.detectChanges();
    await harness.fixture.whenStable();
    setControlValue(element, '[name="recordSourceReference"]', ' https://example.com/docs ');
    linkOption(element, 'Current plan')?.click();
    harness.detectChanges();

    const form = element.querySelector('form.research-discovery-editor') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.addRecord).toHaveBeenCalledWith(featureWithPlans.id, {
      type: 'decision',
      title: 'Documentation behavior',
      content: 'Documentation confirms the behavior.',
      sourceType: 'Web',
      sourceReference: 'https://example.com/docs',
      planIds: ['plan-current'],
    });
  });

  it('updates and removes an existing record', async () => {
    const element = harness.routeNativeElement as HTMLElement;
    const item = element.querySelector('.research-discovery-item') as HTMLElement;
    const buttons = Array.from(item.querySelectorAll<HTMLButtonElement>('header button'));

    buttons.find(button => button.textContent?.trim() === 'Edit')?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(element, '[name="editRecordTitle"]', 'YAML behavior');
    setControlValue(element, '[name="editRecordContent"]', 'YAML selects transitions and validators.');
    await harness.fixture.whenStable();
    const editForm = element.querySelector('.research-discovery-item form') as HTMLFormElement;
    editForm.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.updateRecord).toHaveBeenCalledWith(feature.id, {
      recordId: 'discovery-1',
      type: 'research',
      title: 'YAML behavior',
      content: 'YAML selects transitions and validators.',
      sourceType: 'Code',
      sourceReference: 'StateMachines/features.yaml',
    });

    const removeButton = Array.from(
      element.querySelectorAll<HTMLButtonElement>('.research-discovery-item header button'),
    ).find(button => button.textContent?.trim() === 'Remove');
    removeButton?.click();
    harness.detectChanges();

    expect(features.removeRecord).toHaveBeenCalledWith(feature.id, 'discovery-1');
  });

  it('shows which plans use a record', async () => {
    const element = await openRecordsTab(harness, features, featureWithPlans);
    const items = element.querySelectorAll<HTMLElement>('.research-discovery-item');

    expect(items[0].querySelector('.record-plans')?.textContent).toContain('Current plan');
    expect(items[1].querySelector('.record-plans')).toBeNull();
  });

  it('collapses research content by default and toggles it from the title row', () => {
    const element = harness.routeNativeElement as HTMLElement;
    const discovery = element.querySelector(
      '.research-discovery-document',
    ) as HTMLDetailsElement;
    const title = discovery.querySelector(
      '.research-discovery-toggle',
    ) as HTMLElement;

    expect(discovery.open).toBe(false);
    expect(title.textContent).toContain('YAML configuration');
    expect(title.querySelector('.discovery-chevron')).not.toBeNull();

    title.click();
    harness.detectChanges();
    expect(discovery.open).toBe(true);

    title.click();
    harness.detectChanges();
    expect(discovery.open).toBe(false);
  });

  it('renders review notes as Markdown and submits a new one', async () => {
    const element = await openReviewNotesTab(harness);

    expect(element.textContent).toContain('Review notes');
    expect(element.querySelector('form.review-note-editor')).toBeNull();
    expect(element.textContent).toContain('Replay reloads state');

    element
      .querySelector<HTMLButtonElement>('.section-actions .add-toggle')
      ?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(element, '[name="reviewNoteTitle"]', ' Retry is intended ');
    setControlValue(
      element,
      '[name="reviewNoteContent"]',
      'The projector retries by design. ',
    );

    const form = element.querySelector(
      'form.review-note-editor',
    ) as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.addReviewNote).toHaveBeenCalledWith(feature.id, {
      title: 'Retry is intended',
      content: 'The projector retries by design.',
    });
  });

  it('updates and removes an existing review note', async () => {
    const element = await openReviewNotesTab(harness);
    const item = element.querySelector('.review-note-item') as HTMLElement;

    Array.from(item.querySelectorAll<HTMLButtonElement>('header button'))
      .find(button => button.textContent?.trim() === 'Edit')
      ?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(
      element,
      '[name="editReviewNoteTitle"]',
      'Replay reloads state by design',
    );
    setControlValue(
      element,
      '[name="editReviewNoteContent"]',
      'State replay is intentional and is not a defect.',
    );
    await harness.fixture.whenStable();
    const editForm = element.querySelector(
      '.review-note-item form',
    ) as HTMLFormElement;
    editForm.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.updateReviewNote).toHaveBeenCalledWith(feature.id, {
      reviewNoteId: 'review-note-1',
      title: 'Replay reloads state by design',
      content: 'State replay is intentional and is not a defect.',
    });

    Array.from(
      element.querySelectorAll<HTMLButtonElement>(
        '.review-note-item header button',
      ),
    )
      .find(button => button.textContent?.trim() === 'Remove')
      ?.click();
    harness.detectChanges();

    expect(features.removeReviewNote).toHaveBeenCalledWith(
      feature.id,
      'review-note-1',
    );
  });

  it('shows status, base branch, and additional info on each plans-tab row', async () => {
    const element = await openPlansTab(harness, features);
    const rows = element.querySelectorAll<HTMLElement>('.tab-plan-row');

    expect(rows).toHaveLength(2);
    expect(rows[0].querySelector('.plan-status')?.textContent?.trim()).toBe(
      'In progress',
    );
    expect(rows[0].querySelector('.plan-branch-name')?.textContent).toContain(
      'feature/plans',
    );
    expect(rows[0].querySelector('.plan-row-info')?.textContent).toBe(
      'Production implementation.',
    );
    expect(rows[0].querySelector('.plan-row-records')?.textContent).toContain(
      'YAML configuration',
    );
    expect(rows[1].querySelector('.plan-row-records')).toBeNull();
    expect(rows[1].querySelector('.plan-status')?.textContent?.trim()).toBe(
      'Completed',
    );
    expect(rows[1].querySelector('.plan-branch-name')).toBeNull();
    expect(element.textContent).not.toContain('Make current');
  });

  it('finds plans by sub-branch name', async () => {
    features.watch.mockReturnValue(of(featureWithPlans));
    await harness.navigateByUrl(
      '/features/feature-plans?tab=plans&planSearch=plans-api',
      FeatureDetailsPage,
    );
    harness.detectChanges();
    const element = harness.routeNativeElement as HTMLElement;

    expect(element.querySelectorAll('.tab-plan-row')).toHaveLength(1);
  });

  it('requires confirmation before deleting a plan from the tab', async () => {
    const element = await openPlansTab(harness, features);
    const row = element.querySelector('.tab-plan-row') as HTMLElement;

    planAction(row, 'Delete')?.click();
    harness.detectChanges();

    expect(features.removePlan).not.toHaveBeenCalled();

    planAction(row, 'Confirm')?.click();
    harness.detectChanges();

    expect(features.removePlan).toHaveBeenCalledWith(
      featureWithPlans.id,
      'plan-current',
    );
  });

  it('adds a plan from the collapsed plans-tab editor', async () => {
    const element = await openPlansTab(harness, features);

    expect(element.querySelector('form.plan-editor')).toBeNull();

    element
      .querySelector<HTMLButtonElement>('.section-actions .add-toggle')
      ?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(element, '[name="planTitle"]', ' Follow-up plan ');
    setControlValue(element, '[name="planBranchName"]', ' feature/follow-up ');
    setControlValue(element, '[name="planAdditionalInfo"]', ' Research only. ');
    setControlValue(element, '[name="planContent"]', '# Follow-up');
    linkOption(element, 'YAML configuration')?.click();
    harness.detectChanges();

    const form = element.querySelector('form.plan-editor') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.addPlan).toHaveBeenCalledWith(featureWithPlans.id, {
      title: 'Follow-up plan',
      previousPlanId: null,
      branchName: 'feature/follow-up',
      branchDescription: '',
      status: 'NotStarted',
      additionalInfo: 'Research only.',
      contentType: 'Markdown',
      content: '# Follow-up',
      recordIds: ['discovery-1'],
      branchSources: [],
    });
  });

  it('keeps the record editor collapsed until Add record', async () => {
    const element = harness.routeNativeElement as HTMLElement;
    const toggle = element.querySelector('.section-actions .add-toggle') as HTMLButtonElement;

    expect(element.querySelector('[name="recordTitle"]')).toBeNull();
    expect(toggle.textContent?.trim()).toBe('Add record');

    toggle.click();
    harness.detectChanges();

    expect(element.querySelector('[name="recordTitle"]')).not.toBeNull();
    expect(
      element.querySelector('.section-actions .add-toggle')?.textContent?.trim(),
    ).toBe('Cancel');
  });

  it('keeps tab-only list state on the client without reloading the feature', async () => {
    await harness.navigateByUrl(
      '/features/feature-1?tab=records&recordType=decision',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    expect(element.textContent).toContain('No records match');
    expect(features.watch).toHaveBeenCalledTimes(1);
  });
});

async function openRecordsTab(
  harness: RouterTestingHarness,
  features: { watch: ReturnType<typeof vi.fn> },
  shown: Feature,
): Promise<HTMLElement> {
  features.watch.mockReturnValue(of(shown));
  await harness.navigateByUrl(
    `/features/${shown.id}-records?tab=records`,
    FeatureDetailsPage,
  );
  harness.detectChanges();
  await harness.fixture.whenStable();
  harness.detectChanges();

  return harness.routeNativeElement as HTMLElement;
}

function linkOption(
  element: HTMLElement,
  label: string,
): HTMLInputElement | undefined {
  return Array.from(element.querySelectorAll<HTMLElement>('.link-option'))
    .find(option => option.textContent?.includes(label))
    ?.querySelector<HTMLInputElement>('input[type="checkbox"]') ?? undefined;
}

async function openReviewNotesTab(
  harness: RouterTestingHarness,
): Promise<HTMLElement> {
  await harness.navigateByUrl(
    '/features/feature-1?tab=review-notes',
    FeatureDetailsPage,
  );
  harness.detectChanges();
  await harness.fixture.whenStable();
  harness.detectChanges();

  return harness.routeNativeElement as HTMLElement;
}

async function openPlansTab(
  harness: RouterTestingHarness,
  features: { watch: ReturnType<typeof vi.fn> },
): Promise<HTMLElement> {
  features.watch.mockReturnValue(of(featureWithPlans));
  await harness.navigateByUrl(
    '/features/feature-plans?tab=plans',
    FeatureDetailsPage,
  );
  harness.detectChanges();
  await harness.fixture.whenStable();
  harness.detectChanges();

  return harness.routeNativeElement as HTMLElement;
}

function planAction(
  row: HTMLElement,
  label: string,
): HTMLButtonElement | undefined {
  return Array.from(
    row.querySelectorAll<HTMLButtonElement>('.plan-row-actions button'),
  ).find(button => button.textContent?.trim() === label);
}

function setControlValue(
  element: HTMLElement,
  selector: string,
  value: string,
  eventName: 'input' | 'change' = 'input',
): void {
  const control = element.querySelector(selector) as
    | HTMLInputElement
    | HTMLSelectElement
    | HTMLTextAreaElement;
  control.value = value;
  control.dispatchEvent(new Event(eventName));
}
