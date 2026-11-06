import { AfterViewInit, Directive, ElementRef, inject } from '@angular/core';

/** Focuses (and selects) an inline input as soon as it is rendered. */
@Directive({
  selector: 'input[appAutoFocus]',
  standalone: true,
})
export class AutoFocusDirective implements AfterViewInit {
  private readonly element = inject<ElementRef<HTMLInputElement>>(ElementRef);

  ngAfterViewInit(): void {
    this.element.nativeElement.focus();
    this.element.nativeElement.select();
  }
}
