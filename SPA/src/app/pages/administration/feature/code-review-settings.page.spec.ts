import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { EntityStore } from '../../../core/store/entity-store.service';
import { CodeReviewSettingsPage } from './code-review-settings.page';

describe('CodeReviewSettingsPage', () => {
  let fixture: ComponentFixture<CodeReviewSettingsPage>;
  let http: HttpTestingController;
  const endpoint = '/api/settings/code-review';
  const initial = { batchSize: 5, taskMessage: 'Review the supplied skill or feature instructions.' };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CodeReviewSettingsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(CodeReviewSettingsPage);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  async function load(): Promise<void> {
    http.expectOne(endpoint).flush(initial);
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function input(selector: string, value: string): void {
    const element = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    element.value = value;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  it('loads settings, validates edits and saves once while updating the shared store', async () => {
    await load();
    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect((fixture.nativeElement.querySelector('input') as HTMLInputElement).value).toBe('5');
    input('input', '0');
    expect(button.disabled).toBe(true);
    input('input', '2.5');
    expect(button.disabled).toBe(true);
    input('input', '3');
    input('textarea', '  ');
    expect(button.disabled).toBe(true);
    input('textarea', 'Updated review task');
    button.click();
    button.click();
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    const request = http.expectOne(endpoint);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ batchSize: 3, taskMessage: 'Updated review task' });
    request.flush(request.request.body);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Settings saved');
    expect(await firstValueFrom(TestBed.inject(EntityStore).entity$('code-review-settings', 'global')))
      .toEqual({ id: 'global', batchSize: 3, taskMessage: 'Updated review task' });
  });

  it('shows a load error and can try again', async () => {
    http.expectOne(endpoint).flush({}, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    await load();
    expect(fixture.nativeElement.querySelector('textarea').value).toBe(initial.taskMessage);
  });

  it('preserves edits after a save failure and allows another attempt', async () => {
    await load();
    input('textarea', 'Keep this edit');
    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    http.expectOne(endpoint).flush({}, { status: 500, statusText: 'Failed' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Could not save reviewer settings');
    expect(fixture.nativeElement.querySelector('textarea').value).toBe('Keep this edit');
    (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    http.expectOne(endpoint).flush({ ...initial, taskMessage: 'Keep this edit' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Settings saved');
  });
});
