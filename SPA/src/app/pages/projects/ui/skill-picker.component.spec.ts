import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SkillListItem } from '../../skills/data-access/skill.models';
import { SkillService } from '../../skills/data-access/skill.service';
import { SkillPickerComponent } from './skill-picker.component';

const skill = (id: string, name: string): SkillListItem => ({
  id,
  name,
  description: `${name} description`,
  tags: [],
  referenceCount: 0,
  attachmentCount: 0,
});

describe('SkillPickerComponent', () => {
  const angular = skill('skill-1', 'angular-code-writter');
  const plans = skill('skill-2', 'write-feature-plans');
  const search = vi.fn(() =>
    of({
      items: [angular, plans],
      page: 1,
      pageSize: 8,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    }),
  );

  beforeEach(async () => {
    vi.useFakeTimers();
    search.mockClear();
    await TestBed.configureTestingModule({
      imports: [SkillPickerComponent],
      providers: [{ provide: SkillService, useValue: { search } }],
    }).compileComponents();
  });

  afterEach(() => vi.useRealTimers());

  async function typeInto(input: HTMLInputElement, value: string, fixture: { detectChanges(): void }) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(300);
    fixture.detectChanges();
  }

  it('searches the skills API while typing and picks a result with the keyboard', async () => {
    const fixture = TestBed.createComponent(SkillPickerComponent);
    const picked: (SkillListItem | null)[] = [];
    fixture.componentInstance.skillPicked.subscribe(value => picked.push(value));
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;

    await typeInto(input, 'write', fixture);

    expect(search).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'write' }));
    expect(
      Array.from(element.querySelectorAll('.result strong')).map(item => item.textContent),
    ).toEqual(['angular-code-writter', 'write-feature-plans']);

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(picked.at(-1)).toEqual(plans);
    expect(input.value).toBe('write-feature-plans');
  });

  it('picks a result with the mouse and forgets it when the text changes', async () => {
    const fixture = TestBed.createComponent(SkillPickerComponent);
    const picked: (SkillListItem | null)[] = [];
    fixture.componentInstance.skillPicked.subscribe(value => picked.push(value));
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;

    await typeInto(input, 'ang', fixture);
    (element.querySelector('.result') as HTMLButtonElement).dispatchEvent(
      new MouseEvent('mousedown'),
    );
    fixture.detectChanges();

    expect(picked.at(-1)).toEqual(angular);

    await typeInto(input, 'angular-x', fixture);

    expect(picked.at(-1)).toBeNull();
  });
});
