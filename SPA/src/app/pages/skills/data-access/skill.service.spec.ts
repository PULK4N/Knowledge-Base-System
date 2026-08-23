import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { SkillDto } from './skill.models';
import { SkillService } from './skill.service';

const skill: SkillDto = {
  id: 'skill-1',
  isDeleted: false,
  name: 'Angular writer',
  description: 'Writes Angular features.',
  content: '# Angular',
  tags: ['angular'],
  references: {
    'references/architecture.md': {
      content: '# Architecture',
      loadAutomatically: false,
    },
  },
  attachments: {},
};

describe('SkillService', () => {
  let service: SkillService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SkillService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('maps every list option to the API and caches the projected result', async () => {
    const resultPromise = firstValueFrom(
      service.search({
        page: 2,
        pageSize: 10,
        search: ' writer ',
        tag: ' angular ',
        hasReferences: true,
        hasAttachments: false,
        sortBy: 'ReferenceCount',
        sortDirection: 'Descending',
      }),
    );
    const request = http.expectOne(
      candidate =>
        candidate.url === '/api/skills' &&
        candidate.params.get('page') === '2' &&
        candidate.params.get('pageSize') === '10' &&
        candidate.params.get('search') === 'writer' &&
        candidate.params.get('tag') === 'angular' &&
        candidate.params.get('hasReferences') === 'true' &&
        candidate.params.get('hasAttachments') === 'false' &&
        candidate.params.get('sortBy') === 'ReferenceCount' &&
        candidate.params.get('sortDirection') === 'Descending',
    );

    expect(request.request.method).toBe('GET');
    request.flush({
      items: [
        {
          skillId: skill.id,
          name: skill.name,
          description: skill.description,
          tags: skill.tags,
          referenceCount: 1,
          attachmentCount: 0,
        },
      ],
      page: 2,
      pageSize: 10,
      totalCount: 11,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });

    await expect(resultPromise).resolves.toEqual({
      items: [
        {
          id: skill.id,
          name: skill.name,
          description: skill.description,
          tags: skill.tags,
          referenceCount: 1,
          attachmentCount: 0,
        },
      ],
      page: 2,
      pageSize: 10,
      totalCount: 11,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    });
  });

  it('creates and refreshes a skill', async () => {
    const request = {
      name: 'Event sourcing writer',
      description: 'Writes event-sourced modules.',
      content: '# Event sourcing',
      tags: ['event-sourcing'],
    };
    const resultPromise = firstValueFrom(service.create(request));
    const creation = http.expectOne('/api/skills');

    expect(creation.request.method).toBe('POST');
    expect(creation.request.body).toEqual(request);
    creation.flush({ status: 'OK', skillId: 'skill-2' });

    const refresh = http.expectOne('/api/skills/skill-2');
    refresh.flush({ ...skill, ...request, id: 'skill-2' });

    await expect(resultPromise).resolves.toEqual({
      ...skill,
      ...request,
      id: 'skill-2',
      referenceCount: 1,
      attachmentCount: 0,
    });
  });

  it('updates and refreshes a skill', async () => {
    const request = {
      name: 'Angular code writer',
      description: 'Updated guidance.',
      content: '# Updated',
      tags: ['angular', 'rxjs'],
    };
    const resultPromise = firstValueFrom(service.update(skill.id, request));
    const update = http.expectOne('/api/skills/skill-1/update');

    expect(update.request.method).toBe('POST');
    expect(update.request.body).toEqual(request);
    update.flush({ status: 'OK' });

    const refresh = http.expectOne('/api/skills/skill-1');
    refresh.flush({ ...skill, ...request });

    await expect(resultPromise).resolves.toEqual({
      ...skill,
      ...request,
      referenceCount: 1,
      attachmentCount: 0,
    });
  });

  it('deletes a skill through a POST action', async () => {
    const resultPromise = firstValueFrom(service.delete(skill.id));
    const request = http.expectOne('/api/skills/skill-1/delete');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ status: 'OK' });

    await expect(resultPromise).resolves.toEqual({ status: 'OK' });
  });

  it('updates and refreshes a reference', async () => {
    const request = {
      relativePath: 'references/architecture.md',
      content: '# Updated architecture',
      loadAutomatically: true,
    };
    const resultPromise = firstValueFrom(
      service.updateReference(skill.id, request),
    );
    const update = http.expectOne('/api/skills/skill-1/references/update');

    expect(update.request.method).toBe('POST');
    expect(update.request.body).toEqual(request);
    update.flush({ status: 'OK' });

    const refresh = http.expectOne('/api/skills/skill-1');
    refresh.flush({
      ...skill,
      references: {
        [request.relativePath]: {
          content: request.content,
          loadAutomatically: request.loadAutomatically,
        },
      },
    });

    const result = await resultPromise;
    expect(result.references[request.relativePath]).toEqual({
      content: request.content,
      loadAutomatically: true,
    });
  });

  it('adds and refreshes a reference', async () => {
    const request = {
      relativePath: 'references/testing.md',
      content: '# Testing',
      loadAutomatically: false,
    };
    const resultPromise = firstValueFrom(
      service.addReference(skill.id, request),
    );
    const creation = http.expectOne('/api/skills/skill-1/references');

    expect(creation.request.method).toBe('POST');
    expect(creation.request.body).toEqual(request);
    creation.flush({ status: 'OK' });

    const refresh = http.expectOne('/api/skills/skill-1');
    refresh.flush({
      ...skill,
      references: {
        ...skill.references,
        [request.relativePath]: {
          content: request.content,
          loadAutomatically: request.loadAutomatically,
        },
      },
    });

    const result = await resultPromise;
    expect(result.references[request.relativePath]).toEqual({
      content: request.content,
      loadAutomatically: false,
    });
  });

  it('uploads attachments and refreshes the skill', async () => {
    const file = new File(['content'], 'testing.md', { type: 'text/markdown' });
    const resultPromise = firstValueFrom(
      service.addAttachments(skill.id, [file]),
    );
    const upload = http.expectOne('/api/skills/skill-1/attachments');

    expect(upload.request.method).toBe('POST');
    expect(upload.request.body).toBeInstanceOf(FormData);
    expect((upload.request.body as FormData).getAll('files')).toEqual([file]);
    upload.flush([
      {
        id: 'attachment-1',
        name: 'testing.md',
        size: file.size,
        fileType: file.type,
        extension: '.md',
      },
    ]);

    const refresh = http.expectOne('/api/skills/skill-1');
    refresh.flush(skill);

    await expect(resultPromise).resolves.toEqual({
      ...skill,
      referenceCount: 1,
      attachmentCount: 0,
    });
  });

  it('deletes and refreshes a reference', async () => {
    const relativePath = 'references/architecture.md';
    const resultPromise = firstValueFrom(
      service.deleteReference(skill.id, relativePath),
    );
    const deletion = http.expectOne(
      '/api/skills/skill-1/references/delete',
    );

    expect(deletion.request.method).toBe('POST');
    expect(deletion.request.body).toEqual({ relativePath });
    deletion.flush({ status: 'OK' });

    const refresh = http.expectOne('/api/skills/skill-1');
    refresh.flush({ ...skill, references: {} });

    const result = await resultPromise;
    expect(result.references).toEqual({});
  });
});
