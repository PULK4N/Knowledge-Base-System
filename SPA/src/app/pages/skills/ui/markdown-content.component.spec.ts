import { qualifyMarkdownFragmentLinks } from './markdown-content.component';

describe('qualifyMarkdownFragmentLinks', () => {
  it('keeps README fragments on the current route', () => {
    const container = document.createElement('div');
    container.innerHTML = [
      '<a href="#overview">Overview</a>',
      '<a href="docs/architecture.md">Architecture</a>',
      '<a href="https://example.com/#overview">External</a>',
    ].join('');

    qualifyMarkdownFragmentLinks(
      container,
      '/skills/01a00cdd-c2b1-7297-a1af-6b2cd50a74cd?tab=content',
    );

    const links = container.querySelectorAll<HTMLAnchorElement>('a');
    expect(links[0].getAttribute('href')).toBe(
      '/skills/01a00cdd-c2b1-7297-a1af-6b2cd50a74cd?tab=content#overview',
    );
    expect(links[1].getAttribute('href')).toBe('docs/architecture.md');
    expect(links[2].getAttribute('href')).toBe(
      'https://example.com/#overview',
    );
  });
});
