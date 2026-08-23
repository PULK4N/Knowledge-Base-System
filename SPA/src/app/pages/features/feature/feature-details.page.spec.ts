import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
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
  currentPlanId: null,
  planCount: 0,
  recordCount: 1,
  relatedSkillIds: [],
  records: [
    {
      id: 'record-1',
      userMessage: 'Can this be **Markdown**?',
      aiAnswer: 'Yes.',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  researchDiscoveries: [
    {
      id: 'discovery-1',
      title: 'YAML configuration',
      content: '## Finding\n\nFeature transitions are configured in **YAML**.',
      sourceType: 'Code',
      sourceReference: 'StateMachines/features.yaml',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T11:00:00Z',
    },
  ],
  plans: [],
};

describe('FeatureDetailsPage research discoveries', () => {
  let harness: RouterTestingHarness;
  let features: {
    watch: ReturnType<typeof vi.fn>;
    addResearchDiscovery: ReturnType<typeof vi.fn>;
    updateResearchDiscovery: ReturnType<typeof vi.fn>;
    removeResearchDiscovery: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    features = {
      watch: vi.fn(() => of(feature)),
      addResearchDiscovery: vi.fn(() => of(feature)),
      updateResearchDiscovery: vi.fn(() => of(feature)),
      removeResearchDiscovery: vi.fn(() => of(feature)),
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
                items: [],
                page: 1,
                pageSize: 100,
                totalCount: 0,
                totalPages: 0,
                hasPreviousPage: false,
                hasNextPage: false,
              }),
            ),
          },
        },
      ],
    }).compileComponents();

    harness = await RouterTestingHarness.create(
      '/features/feature-1?tab=research',
    );
  });

  it('renders provenance and submits a new research discovery', () => {
    const element = harness.routeNativeElement as HTMLElement;
    expect(element.textContent).toContain('Research discoveries');
    expect(element.textContent).toContain('YAML configuration');
    expect(element.textContent).toContain(
      'Feature transitions are configured in YAML.',
    );
    expect(
      element.querySelector('.research-discovery-body .markdown-document h2')
        ?.textContent,
    ).toBe('Finding');
    expect(
      element.querySelector('.research-discovery-body .markdown-document strong')
        ?.textContent,
    ).toBe('YAML');
    expect(element.textContent).toContain('StateMachines/features.yaml');

    setControlValue(
      element,
      '[name="researchDiscoveryTitle"]',
      'Documentation behavior ',
    );
    setControlValue(
      element,
      '[name="researchDiscoveryContent"]',
      'Documentation confirms the behavior. ',
    );
    setControlValue(
      element,
      '[name="researchDiscoverySourceType"]',
      'Web',
      'change',
    );
    setControlValue(
      element,
      '[name="researchDiscoverySourceReference"]',
      ' https://example.com/docs ',
    );

    const form = element.querySelector(
      'form.research-discovery-editor',
    ) as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.addResearchDiscovery).toHaveBeenCalledWith(feature.id, {
      title: 'Documentation behavior',
      content: 'Documentation confirms the behavior.',
      sourceType: 'Web',
      sourceReference: 'https://example.com/docs',
    });
  });

  it('updates and removes an existing research discovery', async () => {
    const element = harness.routeNativeElement as HTMLElement;
    const item = element.querySelector(
      '.research-discovery-item',
    ) as HTMLElement;
    const buttons = Array.from(
      item.querySelectorAll<HTMLButtonElement>('header button'),
    );

    buttons.find(button => button.textContent?.trim() === 'Edit')?.click();
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    setControlValue(
      element,
      '[name="editResearchDiscoveryTitle"]',
      'YAML behavior',
    );
    setControlValue(
      element,
      '[name="editResearchDiscoveryContent"]',
      'YAML selects transitions and validators.',
    );
    await harness.fixture.whenStable();
    const editForm = element.querySelector(
      '.research-discovery-item form',
    ) as HTMLFormElement;
    editForm.dispatchEvent(new Event('submit'));
    harness.detectChanges();

    expect(features.updateResearchDiscovery).toHaveBeenCalledWith(
      feature.id,
      {
        discoveryId: 'discovery-1',
        title: 'YAML behavior',
        content: 'YAML selects transitions and validators.',
        sourceType: 'Code',
        sourceReference: 'StateMachines/features.yaml',
      },
    );

    const removeButton = Array.from(
      element.querySelectorAll<HTMLButtonElement>(
        '.research-discovery-item header button',
      ),
    ).find(button => button.textContent?.trim() === 'Remove');
    removeButton?.click();
    harness.detectChanges();

    expect(features.removeResearchDiscovery).toHaveBeenCalledWith(
      feature.id,
      'discovery-1',
    );
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

  it('keeps tab-only list state on the client without reloading the feature', async () => {
    await harness.navigateByUrl(
      '/features/feature-1?tab=research&researchSource=Web',
      FeatureDetailsPage,
    );
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    expect(element.textContent).toContain('No discoveries match');
    expect(features.watch).toHaveBeenCalledTimes(1);
  });

  it('renders long and short conversation record fields as Markdown documents', async () => {
    await harness.navigateByUrl(
      '/features/feature-1?tab=conversations',
      FeatureDetailsPage,
    );
    harness.detectChanges();
    await harness.fixture.whenStable();
    harness.detectChanges();

    const element = harness.routeNativeElement as HTMLElement;
    const documents = element.querySelectorAll(
      '.record-item app-markdown-content.readme-document',
    );

    expect(documents).toHaveLength(2);
    expect(documents[0].querySelector('strong')?.textContent).toBe('Markdown');
    expect(documents[1].querySelector('p')?.textContent).toBe('Yes.');
  });
});

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
