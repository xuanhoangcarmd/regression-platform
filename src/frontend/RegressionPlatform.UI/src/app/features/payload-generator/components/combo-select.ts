import {
  Component,
  Input,
  ElementRef,
  ViewChild,
  HostListener,
  forwardRef,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

export interface ComboOption {
  id: number;
  name: string;
}

/**
 * Searchable id+name dropdown (ported from payload-v6-generator). Renders `id - name`,
 * accepts a raw numeric id typed manually, and filters by name or id. Styling relies on the
 * `.input` class provided by the host payload-generator page (ViewEncapsulation.None, .pg-root).
 */
@Component({
  selector: 'pg-combo-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ComboSelectComponent),
      multi: true,
    },
  ],
  template: `
    <div class="relative">
      <input
        #inputEl
        type="text"
        [placeholder]="placeholder"
        [(ngModel)]="searchText"
        (focus)="onFocus()"
        (input)="onSearch()"
        (keydown)="onKeydown($event)"
        class="input font-mono w-full pr-8"
        autocomplete="off"
      />
      <button
        type="button"
        (click)="toggleDropdown()"
        class="absolute right-1 top-1/2 -translate-y-1/2 p-1 text-secondary hover:text-on-surface"
        tabindex="-1"
      >
        <svg class="w-3.5 h-3.5" viewBox="0 0 20 20" fill="currentColor">
          <path
            fill-rule="evenodd"
            d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z"
            clip-rule="evenodd"
          />
        </svg>
      </button>

      @if (isOpen && filtered.length > 0) {
        <div
          class="absolute z-50 mt-1 w-full max-h-48 overflow-auto rounded-lg shadow-xl border border-outline-variant bg-surface-container-lowest"
        >
          @for (opt of filtered; track opt.id; let i = $index) {
            <button
              type="button"
              (mousedown)="select(opt)"
              class="w-full text-left px-3 py-1.5 text-body-sm text-on-surface transition-colors hover:bg-surface-container-low"
              [class]="
                (i === highlightIdx ? 'bg-surface-container-low' : '') +
                (opt.id === value ? ' text-primary font-medium' : '')
              "
            >
              <span class="font-code-sm text-secondary mr-1.5">{{ opt.id }}</span>
              <span>{{ opt.name }}</span>
            </button>
          }
        </div>
      }
    </div>
  `,
})
export class ComboSelectComponent implements ControlValueAccessor, OnChanges {
  @Input() options: ComboOption[] = [];
  @Input() placeholder = 'Search or enter ID...';
  /** Display name used when the current value isn't in `options` (e.g. TPMS-variant
   * itemIds = base + 0x8000 produced by the DLL at runtime, not stored in DB). */
  @Input() fallbackName = '';
  @ViewChild('inputEl') inputEl!: ElementRef<HTMLInputElement>;

  value: number = 0;
  searchText = '';
  isOpen = false;
  filtered: ComboOption[] = [];
  highlightIdx = -1;

  private onChange?: (v: number) => void;
  private onTouched?: () => void;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['options'] || changes['fallbackName']) {
      this.syncDisplay();
    }
  }

  writeValue(val: number): void {
    this.value = val ?? 0;
    this.syncDisplay();
  }

  registerOnChange(fn: (v: number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  private syncDisplay(): void {
    const match = this.options.find((o) => o.id === this.value);
    if (match) {
      this.searchText = `${match.id} - ${match.name}`;
    } else if (this.value && this.value !== 65535) {
      this.searchText = this.fallbackName
        ? `${this.value} - ${this.fallbackName}`
        : `${this.value}`;
    } else {
      this.searchText = '';
    }
  }

  onFocus(): void {
    this.searchText = '';
    this.isOpen = true;
    this.filtered = this.options.slice(0, 200);
    this.highlightIdx = -1;
  }

  onSearch(): void {
    this.isOpen = true;
    this.highlightIdx = -1;
    const q = this.searchText.toLowerCase().trim();
    if (!q) {
      this.filtered = this.options.slice(0, 200);
      return;
    }
    this.filtered = this.options
      .filter((o) => o.name.toLowerCase().includes(q) || String(o.id).includes(q))
      .slice(0, 200);
  }

  select(opt: ComboOption): void {
    this.value = opt.id;
    this.searchText = `${opt.id} - ${opt.name}`;
    this.isOpen = false;
    this.onChange?.(this.value);
  }

  toggleDropdown(): void {
    if (this.isOpen) {
      this.close();
    } else {
      this.inputEl.nativeElement.focus();
    }
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.highlightIdx = Math.min(this.highlightIdx + 1, this.filtered.length - 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.highlightIdx = Math.max(this.highlightIdx - 1, 0);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (this.highlightIdx >= 0 && this.highlightIdx < this.filtered.length) {
        this.select(this.filtered[this.highlightIdx]);
      } else {
        this.commitManual();
      }
    } else if (e.key === 'Escape') {
      this.close();
    }
  }

  private commitManual(): void {
    const num = parseInt(this.searchText, 10);
    if (!isNaN(num)) {
      this.value = num;
      this.onChange?.(this.value);
    }
    this.close();
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(e: Event): void {
    if (!this.inputEl?.nativeElement.closest('.relative')?.contains(e.target as Node)) {
      if (this.isOpen) this.close();
    }
  }

  private close(): void {
    this.isOpen = false;
    this.tryCommitManual();
    this.syncDisplay();
    this.onTouched?.();
  }

  private tryCommitManual(): void {
    const text = this.searchText.trim();
    if (!text) return;
    // If user typed a raw number, commit it
    const num = parseInt(text, 10);
    if (!isNaN(num) && num !== this.value) {
      this.value = num;
      this.onChange?.(this.value);
    }
  }
}
