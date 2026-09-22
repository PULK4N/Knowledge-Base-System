import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Observable,
  catchError,
  distinctUntilChanged,
  filter,
  map,
  of,
  shareReplay,
  startWith,
  switchMap,
} from 'rxjs';
import { LoadState, toUserMessage } from '../../../core/http/load-state';
import { MarkdownContentComponent } from '../../skills/ui/markdown-content.component';
import {
  MemoryConversation,
  MemoryConversationMessage,
  MemoryMessageRole,
  MemoryToolCall,
} from '../data-access/memory.models';
import { MemoryService } from '../data-access/memory.service';
import { memoryTitle } from './memory-title';

const ROLE_LABELS: Readonly<Record<MemoryMessageRole, string>> = {
  user: 'Prompt',
  assistant: 'Assistant',
  hook: 'Hook',
};

interface MemoryChatMessage extends MemoryConversationMessage {
  readonly toolCalls: readonly MemoryToolCall[];
}

interface MemoryChatView {
  readonly conversation: MemoryConversation;
  readonly title: string;
  readonly messages: readonly MemoryChatMessage[];
}

// Groups the tool calls of a prompt under the message that started it, so a
// reader sees what the agent executed for that prompt.
function toChatMessages(
  conversation: MemoryConversation,
): MemoryChatMessage[] {
  const toolCallsByPrompt = new Map<string, MemoryToolCall[]>();

  for (const toolCall of conversation.toolCalls) {
    const promptToolCalls = toolCallsByPrompt.get(toolCall.promptId);

    if (promptToolCalls) {
      promptToolCalls.push(toolCall);
    } else {
      toolCallsByPrompt.set(toolCall.promptId, [toolCall]);
    }
  }

  const owners = new Map<string, MemoryConversationMessage>();

  for (const message of conversation.messages) {
    const owner = owners.get(message.promptId);

    if (!owner || (owner.role !== 'user' && message.role === 'user')) {
      owners.set(message.promptId, message);
    }
  }

  return conversation.messages.map(message => ({
    ...message,
    toolCalls:
      owners.get(message.promptId)?.id === message.id
        ? (toolCallsByPrompt.get(message.promptId) ?? [])
        : [],
  }));
}

@Component({
  selector: 'app-memory-chat-page',
  imports: [AsyncPipe, DatePipe, MarkdownContentComponent, RouterLink],
  templateUrl: './memory-chat.page.html',
  styleUrl: './memory-chat.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemoryChatPage {
  private readonly route = inject(ActivatedRoute);
  private readonly memories = inject(MemoryService);

  protected readonly emptyBlocks = [];

  protected readonly state$: Observable<LoadState<MemoryChatView>> =
    this.route.paramMap.pipe(
      map(params => params.get('memoryId')),
      filter((memoryId): memoryId is string => memoryId !== null),
      distinctUntilChanged(),
      switchMap(memoryId =>
        this.memories.getConversation(memoryId).pipe(
          map(conversation => ({
            status: 'success',
            data: {
              conversation,
              title: memoryTitle(conversation),
              messages: toChatMessages(conversation),
            },
          }) as const),
          startWith({ status: 'loading' } as const),
          catchError(error =>
            of({
              status: 'error',
              message: toUserMessage(error),
            } as const),
          ),
        ),
      ),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

  protected roleLabel(role: MemoryMessageRole): string {
    return ROLE_LABELS[role];
  }
}
