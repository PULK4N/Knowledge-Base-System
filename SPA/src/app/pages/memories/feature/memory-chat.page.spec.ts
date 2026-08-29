import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { provideKnowledgeMarkdown } from '../../../shared/markdown/markdown.providers';
import { MemoryConversation } from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import { MemoryChatPage } from './memory-chat.page';

const conversation: MemoryConversation = {
  memoryId: 'memory-1',
  threadId: '33333333-3333-3333-3333-333333333333',
  summary: 'The chat refactored the outbox.',
  summaryTimestamp: '2026-08-22T12:00:00Z',
  firstPromptTimestamp: '2026-08-22T10:00:00Z',
  lastPromptTimestamp: '2026-08-22T11:00:00Z',
  messages: [
    {
      id: 'prompt-1:0',
      promptId: 'prompt-1',
      hookIndex: 0,
      timestamp: '2026-08-22T10:00:00Z',
      hookEventName: 'UserPromptSubmit',
      role: 'user',
      message: 'Refactor the **outbox**',
      payloadJson: '{\n  "session_id": "019f"\n}',
    },
    {
      id: 'prompt-1:1',
      promptId: 'prompt-1',
      hookIndex: 1,
      timestamp: '2026-08-22T10:00:00Z',
      hookEventName: 'Stop',
      role: 'assistant',
      message: 'Refactored it.',
      payloadJson: '{\n  "session_id": "019f"\n}',
    },
    {
      id: 'prompt-2:0',
      promptId: 'prompt-2',
      hookIndex: 0,
      timestamp: '2026-08-22T11:00:00Z',
      hookEventName: 'SessionStart',
      role: 'hook',
      message: '',
      payloadJson: '{\n  "source": "compact"\n}',
    },
  ],
};

describe('MemoryChatPage', () => {
  let harness: RouterTestingHarness;
  let memories: { getConversation: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    memories = { getConversation: vi.fn(() => of(conversation)) };

    await TestBed.configureTestingModule({
      imports: [MemoryChatPage],
      providers: [
        provideRouter([
          { path: 'memories/:memoryId', component: MemoryChatPage },
        ]),
        ...provideKnowledgeMarkdown(),
        { provide: MemoryService, useValue: memories },
      ],
    }).compileComponents();

    harness = await RouterTestingHarness.create('/memories/memory-1');
  });

  it('renders prompts and assistant messages as a Markdown chat', () => {
    const element = harness.routeNativeElement as HTMLElement;

    expect(memories.getConversation).toHaveBeenCalledWith('memory-1');
    const messages = Array.from(element.querySelectorAll('.chat-message'));
    expect(messages).toHaveLength(3);
    expect(messages[0].classList).toContain('role-user');
    expect(messages[1].classList).toContain('role-assistant');
    expect(
      messages[0].querySelector('.markdown-document strong')?.textContent,
    ).toBe('outbox');
    expect(messages[1].textContent).toContain('Refactored it.');
    expect(element.textContent).toContain('Thread 33333333');
  });

  it('keeps the raw payload collapsed until it is expanded', () => {
    const element = harness.routeNativeElement as HTMLElement;
    const payload = element.querySelector(
      '.chat-message .message-payload',
    ) as HTMLDetailsElement;

    expect(payload.open).toBe(false);
    expect(payload.textContent).toContain('Raw payload');

    payload.open = true;
    harness.detectChanges();

    expect(payload.querySelector('pre')?.textContent).toContain('session_id');
  });

  it('explains a hook that carries no conversation text', () => {
    const element = harness.routeNativeElement as HTMLElement;
    const hookMessage = element.querySelectorAll('.chat-message')[2];

    expect(hookMessage.classList).toContain('role-hook');
    expect(hookMessage.textContent).toContain(
      'This hook carries no prompt or assistant message.',
    );
    expect(hookMessage.textContent).toContain('SessionStart');
  });
});
