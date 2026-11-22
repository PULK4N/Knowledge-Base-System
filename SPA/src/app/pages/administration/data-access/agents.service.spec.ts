import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, take, toArray } from 'rxjs';
import { Agent } from './agents.models';
import { AgentsService } from './agents.service';

const codex: Agent = {
  id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  userName: 'codex',
  displayName: 'Codex',
  isAdministrator: false,
  permissions: ['memory.write'],
};

const claude: Agent = {
  id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  userName: 'claude',
  displayName: 'Claude',
  isAdministrator: true,
  permissions: [],
};

describe('AgentsService', () => {
  let service: AgentsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AgentsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the agents and serves them from the cache next time', async () => {
    const agentsPromise = firstValueFrom(service.watchAll());
    const request = http.expectOne('/api/agents');

    expect(request.request.method).toBe('GET');
    request.flush([codex, claude]);

    await expect(agentsPromise).resolves.toEqual([codex, claude]);

    await expect(firstValueFrom(service.watchAll())).resolves.toEqual([codex, claude]);
  });

  it('creates an agent with trimmed names, then reloads the list', async () => {
    const createdPromise = firstValueFrom(
      service.create({
        userName: ' codex ',
        displayName: '  ',
        isAdministrator: false,
        permissions: ['memory.write'],
      }),
    );
    const create = http.expectOne('/api/agents');

    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      userName: 'codex',
      displayName: null,
      isAdministrator: false,
      permissions: ['memory.write'],
    });
    create.flush(codex);
    const reload = http.expectOne('/api/agents');
    expect(reload.request.method).toBe('GET');
    reload.flush([codex, claude]);

    await expect(createdPromise).resolves.toEqual(codex);
  });

  it('sets permissions and updates the cached agent in the list', async () => {
    const listPromise = firstValueFrom(service.watchAll().pipe(take(2), toArray()));
    http.expectOne('/api/agents').flush([codex]);

    const updatedPromise = firstValueFrom(
      service.setPermissions(codex.id, {
        isAdministrator: true,
        permissions: ['skills.read'],
      }),
    );
    const request = http.expectOne(`/api/agents/${codex.id}/permissions`);

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      isAdministrator: true,
      permissions: ['skills.read'],
    });
    const updated = { ...codex, isAdministrator: true, permissions: ['skills.read'] };
    request.flush(updated);

    await expect(updatedPromise).resolves.toEqual(updated);
    await expect(listPromise).resolves.toEqual([[codex], [updated]]);
  });
});
