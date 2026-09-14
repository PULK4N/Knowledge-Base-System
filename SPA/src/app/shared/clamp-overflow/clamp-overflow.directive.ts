import {
  DestroyRef,
  Directive,
  ElementRef,
  afterEveryRender,
  inject,
  input,
  output,
} from '@angular/core';

/**
 * Reports whether a line-clamped element actually hides content, so a
 * Show more toggle is only offered when it would reveal something.
 *
 * Measurement happens after every render and on element resize while
 * `appClampOverflow` is true; the last measured value is kept while the
 * element is expanded so the Show less toggle stays available.
 */
@Directive({
  selector: '[appClampOverflow]',
  standalone: true,
})
export class ClampOverflowDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly resizeObserver = createResizeObserver(() => this.measure());
  private overflows: boolean | null = null;

  /** True while the element is visually clamped. */
  readonly appClampOverflow = input.required<boolean>();

  readonly overflowChanged = output<boolean>();

  constructor() {
    afterEveryRender(() => this.measure());
    this.resizeObserver?.observe(this.host.nativeElement);
    inject(DestroyRef).onDestroy(() => this.resizeObserver?.disconnect());
  }

  private measure(): void {
    if (!this.appClampOverflow()) return;

    const element = this.host.nativeElement;
    const overflows = element.scrollHeight > element.clientHeight + 1;

    if (overflows !== this.overflows) {
      this.overflows = overflows;
      this.overflowChanged.emit(overflows);
    }
  }
}

function createResizeObserver(callback: () => void): ResizeObserver | null {
  return typeof ResizeObserver === 'undefined'
    ? null
    : new ResizeObserver(callback);
}
