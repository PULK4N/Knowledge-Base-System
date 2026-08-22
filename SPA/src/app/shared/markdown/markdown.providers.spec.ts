import { TestBed } from '@angular/core/testing';
import { MarkdownService } from 'ngx-markdown';
import { provideKnowledgeMarkdown } from './markdown.providers';

describe('provideKnowledgeMarkdown', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideKnowledgeMarkdown()],
    });
  });

  it('renders README-oriented GFM and maintained extensions', async () => {
    const service = TestBed.inject(MarkdownService);
    const html = await service.parse(
      [
        '# README showcase',
        '',
        '> [!WARNING]',
        '> Keep **clear**.',
        '',
        '| Option | Default |',
        '|---|---:|',
        '| `armed` | **true** |',
        '',
        '- Parent',
        '  - Child',
        '- [x] Complete',
        '',
        '<details><summary>More</summary>Expanded</details>',
        '',
        'A statement.[^source]',
        '',
        '[^source]: Supporting context.',
      ].join('\n'),
    );

    expect(html).toContain('<h1 id="readme-showcase">');
    expect(html).toContain('markdown-alert-warning');
    expect(html).toContain('<strong>clear</strong>');
    expect(html).toContain('<table>');
    expect(html).toContain('type="checkbox"');
    expect(html).toContain('<ul>');
    expect(html).toContain('<details>');
    expect(html).toContain('class="footnotes"');
    expect(html).not.toContain('[^source]');
  });

  it('sanitizes unsafe HTML while retaining supported README HTML', async () => {
    const service = TestBed.inject(MarkdownService);
    const html = await service.parse(
      '<img src="image.svg" onerror="alert(1)"><script>alert(1)</script>',
    );

    expect(html).toContain('<img src="image.svg">');
    expect(html).not.toContain('onerror');
    expect(html).not.toContain('<script>');
  });
});
