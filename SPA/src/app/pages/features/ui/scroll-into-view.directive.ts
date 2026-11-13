import { Directive, ElementRef, afterNextRender, inject, input } from '@angular/core';

/**
 * Scrolls its element into view once after it renders when the input is true,
 * so an origin link can land on the plan item it points to.
 */
@Directive({
  selector: '[appScrollIntoView]',
  host: { '[class.scroll-target]': 'appScrollIntoView()' },
})
export class ScrollIntoViewDirective {
  readonly appScrollIntoView = input(false);

  constructor() {
    const element = inject(ElementRef<HTMLElement>);
    afterNextRender(() => {
      if (this.appScrollIntoView())
        element.nativeElement.scrollIntoView({ block: 'center', behavior: 'smooth' });
    });
  }
}
