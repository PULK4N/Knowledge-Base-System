import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { OverviewService } from './overview.service';

describe('OverviewService', () => {
  let service: OverviewService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OverviewService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads finite count requests and combines the overview', async () => {
    const resultPromise = firstValueFrom(service.getCounts());
    const counts = new Map<string, number>([
      ['/api/skills', 4],
      ['/api/policies/general', 3],
      ['/api/policies/projects', 2],
      ['/api/policies/topics', 5],
      ['/api/policies/agent-families', 2],
      ['/api/memories', 7],
    ]);

    for (const [path, totalCount] of counts) {
      const request = http.expectOne(candidate => candidate.url === path);
      expect(request.request.method).toBe('GET');
      expect(request.request.params.get('page')).toBe('1');
      expect(request.request.params.get('pageSize')).toBe('1');
      request.flush({
        items: [],
        page: 1,
        pageSize: 1,
        totalCount,
        totalPages: totalCount > 0 ? 1 : 0,
        hasPreviousPage: false,
        hasNextPage: totalCount > 1,
      });
    }

    await expect(resultPromise).resolves.toEqual({
      skills: 4,
      policies: 12,
      memories: 7,
    });
  });
});
