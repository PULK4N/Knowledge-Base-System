import { parseMarkdownBlocks } from './markdown-blocks';

describe('parseMarkdownBlocks', () => {
  it('turns headings, paragraphs, lists, and code fences into typed blocks', () => {
    const fence = String.fromCharCode(96).repeat(3);
    const blocks = parseMarkdownBlocks(
      [
        '# Build a feature',
        '',
        'Keep the slice focused.',
        '',
        '- Inspect contracts',
        '- Implement',
        '',
        '1. Test',
        '2. Build',
        '',
        fence + 'ts',
        'const safe = true;',
        fence,
      ].join('\n'),
    );

    expect(blocks).toEqual([
      { kind: 'heading', level: 1, text: 'Build a feature' },
      { kind: 'paragraph', text: 'Keep the slice focused.' },
      {
        kind: 'unordered-list',
        items: ['Inspect contracts', 'Implement'],
      },
      { kind: 'ordered-list', items: ['Test', 'Build'] },
      {
        kind: 'code',
        language: 'ts',
        text: 'const safe = true;',
      },
    ]);
  });

  it('keeps HTML-looking server text as plain block content', () => {
    const blocks = parseMarkdownBlocks('<img src=x onerror=alert(1)>');

    expect(blocks).toEqual([
      {
        kind: 'paragraph',
        text: '<img src=x onerror=alert(1)>',
      },
    ]);
  });
});
