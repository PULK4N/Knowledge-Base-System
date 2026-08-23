import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  ParamMap,
  Router,
  convertToParamMap,
} from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { SkillSearchResult } from '../data-access/skill.models';
import { SkillService } from '../data-access/skill.service';
import { SkillsListPage } from './skills-list.page';

describe('SkillsListPage', () => {
  let fixture: ComponentFixture<SkillsListPage>;
  let params: BehaviorSubject<ParamMap>;
  let cancelledRequests: number[];
  let requestNumber: number;

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ search: 'first' }));
    cancelledRequests = [];
    requestNumber = 0;

    TestBed.configureTestingModule({
      imports: [SkillsListPage],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: params.asObservable() },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn() },
        },
        {
          provide: SkillService,
          useValue: {
            search: vi.fn(
              () =>
                new Observable<SkillSearchResult>(() => {
                  const currentRequest = ++requestNumber;
                  return () => cancelledRequests.push(currentRequest);
                }),
            ),
          },
        },
      ],
    });
    TestBed.overrideComponent(SkillsListPage, { set: { template: '' } });
    fixture = TestBed.createComponent(SkillsListPage);
  });

  it('cancels a stale API read when URL list state changes', () => {
    const page = fixture.componentInstance as unknown as {
      readonly vm$: Observable<unknown>;
    };
    const subscription = page.vm$.subscribe();

    expect(requestNumber).toBe(1);
    params.next(
      convertToParamMap({
        search: 'second',
        sortBy: 'ReferenceCount',
        sortDirection: 'Descending',
      }),
    );

    expect(requestNumber).toBe(2);
    expect(cancelledRequests).toEqual([1]);
    subscription.unsubscribe();
  });
});
