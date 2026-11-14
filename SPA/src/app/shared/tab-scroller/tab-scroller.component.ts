import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterEveryRender,
  afterNextRender,
  contentChild,
  inject,
  signal,
} from '@angular/core';

/**
 * Wraps a horizontally scrolling tab strip (marked with `#tabStrip`) and
 * replaces its scrollbar with left/right arrow buttons that only show
 * while there are hidden tabs in that direction.
 *
 * The strip keeps its own styling; it must scroll horizontally
 * (`overflow-x: auto`) and should hide its scrollbar.
 * The selected tab (`aria-selected="true"`) is scrolled into view whenever
 * it changes, so a tab opened from the URL is never hidden.
 */
@Component({
  selector: 'app-tab-scroller',
  templateUrl: './tab-scroller.component.html',
  styleUrl: './tab-scroller.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TabScrollerComponent {
  private readonly strip = contentChild.required<string, ElementRef<HTMLElement>>('tabStrip', {
    read: ElementRef,
  });
  private readonly resizeObserver =
    typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(() => this.measure());
  private selectedTab: Element | null = null;

  protected readonly canScrollStart = signal(false);
  protected readonly canScrollEnd = signal(false);

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      const element = this.strip().nativeElement;
      const onScroll = () => this.measure();

      element.addEventListener('scroll', onScroll, { passive: true });
      this.resizeObserver?.observe(element);
      destroyRef.onDestroy(() => {
        element.removeEventListener('scroll', onScroll);
        this.resizeObserver?.disconnect();
      });
    });

    afterEveryRender(() => {
      this.revealSelectedTab();
      this.measure();
    });
  }

  protected scroll(direction: -1 | 1): void {
    const element = this.strip().nativeElement;
    element.scrollBy({ left: direction * element.clientWidth * 0.7, behavior: 'smooth' });
  }

  private measure(): void {
    const element = this.strip().nativeElement;
    const maxScroll = element.scrollWidth - element.clientWidth;

    this.canScrollStart.set(element.scrollLeft > 1);
    this.canScrollEnd.set(element.scrollLeft < maxScroll - 1);
  }

  private revealSelectedTab(): void {
    const element = this.strip().nativeElement;
    const selected = element.querySelector('[aria-selected="true"]');

    if (!selected || selected === this.selectedTab) return;
    this.selectedTab = selected;

    const stripBox = element.getBoundingClientRect();
    const tabBox = selected.getBoundingClientRect();
    // Leave room for the arrow buttons that overlay the strip edges.
    const margin = 40;

    if (tabBox.left < stripBox.left + margin) {
      element.scrollLeft -= stripBox.left + margin - tabBox.left;
    } else if (tabBox.right > stripBox.right - margin) {
      element.scrollLeft += tabBox.right - (stripBox.right - margin);
    }
  }
}
