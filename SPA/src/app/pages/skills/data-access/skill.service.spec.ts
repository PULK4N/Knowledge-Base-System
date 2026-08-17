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

describe('SkillService mutations', () => {
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

    await expect(resultPromise).resolves.toEqual({ ...skill, ...request });
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
