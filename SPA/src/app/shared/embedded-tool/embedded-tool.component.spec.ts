import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { EmbeddedToolComponent } from './embedded-tool.component';

@Component({
  imports: [EmbeddedToolComponent],
  template: `
    <app-embedded-tool
      toolName="PostgreSQL"
      description="Browse tables"
      url="/pgweb/"
      frameTitle="PostgreSQL database browser"
    />
  `,
})
class EmbeddedToolHost {}

function createHost() {
  const fixture = TestBed.createComponent(EmbeddedToolHost);
  fixture.detectChanges();
  return {
    fixture,
    element: fixture.nativeElement as HTMLElement,
  };
}

function findButton(element: HTMLElement, label: string): HTMLButtonElement {
  const buttons = Array.from(element.querySelectorAll('button'));
  const button = buttons.find(candidate =>
    (candidate.textContent ?? '').includes(label),
  );

  if (!button) {
    throw new Error(`No button labelled ${label}`);
  }

  return button;
}

describe('EmbeddedToolComponent', () => {
  it('frames the tool and links to it in a new tab', () => {
    const { element } = createHost();
    const frame = element.querySelector('iframe') as HTMLIFrameElement;
    const link = element.querySelector('a') as HTMLAnchorElement;

    expect(element.textContent).toContain('PostgreSQL');
    expect(element.textContent).toContain('Browse tables');
    expect(frame.getAttribute('src')).toBe('/pgweb/');
    expect(frame.getAttribute('title')).toBe('PostgreSQL database browser');
    expect(link.getAttribute('href')).toBe('/pgweb/');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
  });

  it('expands to a full-window overlay when the browser refuses full screen', async () => {
    const { fixture, element } = createHost();
    const panel = element.querySelector('.embedded-tool') as HTMLElement;

    expect(panel.classList.contains('overlay')).toBe(false);

    findButton(element, 'Full screen').click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(panel.classList.contains('overlay')).toBe(true);
    expect(
      findButton(element, 'Exit full screen').getAttribute('aria-pressed'),
    ).toBe('true');
  });

  it('leaves the overlay on escape', async () => {
    const { fixture, element } = createHost();
    const panel = element.querySelector('.embedded-tool') as HTMLElement;

    findButton(element, 'Full screen').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(panel.classList.contains('overlay')).toBe(true);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();

    expect(panel.classList.contains('overlay')).toBe(false);
  });

  it('uses the browser full screen API when it is available', async () => {
    const { fixture, element } = createHost();
    const panel = element.querySelector('.embedded-tool') as HTMLElement;
    const requestFullscreen = vi.fn(() => Promise.resolve());
    panel.requestFullscreen = requestFullscreen;

    findButton(element, 'Full screen').click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(requestFullscreen).toHaveBeenCalled();
    expect(panel.classList.contains('overlay')).toBe(false);
  });

  it('reloads the frame without changing its source', () => {
    const { element } = createHost();
    const frame = element.querySelector('iframe') as HTMLIFrameElement;

    findButton(element, 'Reload').click();

    expect(frame.getAttribute('src')).toContain('/pgweb/');
  });
});
