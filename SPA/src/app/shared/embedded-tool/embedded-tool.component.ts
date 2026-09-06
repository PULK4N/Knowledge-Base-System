import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  ElementRef,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { fromEvent, map } from 'rxjs';

/**
 * Embeds an operations tool that is served by another container behind the same
 * origin. The frame can be shown inline, in browser full screen, or in a
 * full-window overlay when the browser refuses the Fullscreen API.
 */
@Component({
  selector: 'app-embedded-tool',
  templateUrl: './embedded-tool.component.html',
  styleUrl: './embedded-tool.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'leaveOverlay()',
  },
})
export class EmbeddedToolComponent {
  private readonly document = inject(DOCUMENT);
  private readonly sanitizer = inject(DomSanitizer);

  readonly toolName = input.required<string>();
  readonly description = input('');
  readonly url = input.required<string>();
  readonly frameTitle = input.required<string>();

  private readonly viewport =
    viewChild.required<ElementRef<HTMLElement>>('viewport');
  private readonly frame =
    viewChild.required<ElementRef<HTMLIFrameElement>>('frame');

  private readonly overlay = signal(false);
  private readonly browserFullscreen = toSignal(
    fromEvent(this.document, 'fullscreenchange').pipe(
      map(() => this.isBrowserFullscreen()),
    ),
    { initialValue: false },
  );

  protected readonly safeUrl = computed<SafeResourceUrl>(() =>
    this.sanitizer.bypassSecurityTrustResourceUrl(this.url()),
  );
  protected readonly overlayActive = this.overlay.asReadonly();
  protected readonly fullscreen = computed(
    () => this.overlay() || this.browserFullscreen(),
  );
  protected readonly fullscreenLabel = computed(() =>
    this.fullscreen() ? 'Exit full screen' : 'Full screen',
  );

  protected reload(): void {
    const frame = this.frame().nativeElement;
    const source = frame.src;
    frame.src = source;
  }

  protected async toggleFullscreen(): Promise<void> {
    if (this.isBrowserFullscreen()) {
      await this.document.exitFullscreen();
      return;
    }

    if (this.overlay()) {
      this.overlay.set(false);
      return;
    }

    try {
      await this.viewport().nativeElement.requestFullscreen();
    } catch {
      this.overlay.set(true);
    }
  }

  protected leaveOverlay(): void {
    this.overlay.set(false);
  }

  private isBrowserFullscreen(): boolean {
    return this.document.fullscreenElement === this.viewport().nativeElement;
  }
}
