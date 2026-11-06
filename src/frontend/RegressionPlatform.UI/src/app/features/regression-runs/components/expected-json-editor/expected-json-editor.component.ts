import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import {
  IJsonTreeRow,
  JsonChange,
  diffJson,
  flattenJson,
  literalText,
  parseLiteral,
  removeAt,
  setAt,
  tryParseJson,
} from '../../helpers/json-edit.helper';
import { IExpectedDraft } from '../../models/expected-response.model';
import { IExpectedEditState } from '../../store/expected-response.store';
import { AutoFocusDirective } from './auto-focus.directive';

type EditorView = 'tree' | 'code';

export type ExpectedEditPatch = Partial<Pick<IExpectedDraft, 'json' | 'statusCode' | 'reason'>>;

/**
 * Edits one request's expected response: a Tree view (click a value to change it, add / remove
 * fields) and a Code view (raw JSON). Changes against the original captured body are highlighted.
 * Stateless about the data — every change is emitted as a patch for the store's draft.
 */
@Component({
  selector: 'app-expected-json-editor',
  standalone: true,
  imports: [MatIconModule, AutoFocusDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './expected-json-editor.component.html',
})
export class ExpectedJsonEditorComponent {
  readonly state = input.required<IExpectedEditState>();
  readonly edit = output<ExpectedEditPatch>();

  protected readonly view = signal<EditorView>('tree');
  protected readonly highlight = signal(true);
  protected readonly collapsed = signal<ReadonlySet<string>>(new Set());
  /** Path whose value is being edited inline (tree view). */
  protected readonly editingPath = signal<string | null>(null);
  /** Object path that shows the "add field" inline form. */
  protected readonly addingPath = signal<string | null>(null);

  protected readonly literalText = literalText;

  /**
   * The Code view's own text. The store drops a draft that only differs in whitespace (same JSON as
   * the saved body), so binding the textarea straight to `state().json` would leave the DOM out of
   * sync (and Format would emit an unchanged value → no re-render). Keep what the user typed while it
   * still means the same JSON as the state; otherwise (tree edit, undo, another request) follow the state.
   */
  private readonly localCode = signal<{ requestId: number; text: string } | null>(null);

  protected readonly codeText = computed(() => {
    const state = this.state();
    const local = this.localCode();
    if (local && local.requestId === state.detail.requestId && (local.text === state.json || this.sameJson(local.text, state.json))) {
      return local.text;
    }
    return state.json;
  });

  protected readonly parsed = computed(() => {
    const json = this.state().json;
    return json.trim() ? tryParseJson(json) : { value: null, error: null };
  });

  protected readonly hasBody = computed(() => this.state().json.trim() !== '');

  protected readonly original = computed(() => {
    const json = this.state().detail.responseJson;
    return json ? tryParseJson(json).value : null;
  });

  protected readonly rows = computed<IJsonTreeRow[]>(() => {
    const parsed = this.parsed();
    return parsed.error || !this.hasBody() ? [] : flattenJson(parsed.value, this.collapsed());
  });

  /** Leaf diff against the original captured body (null when there is nothing to compare). */
  protected readonly diff = computed(() => {
    const parsed = this.parsed();
    const original = this.original();
    if (!this.highlight() || parsed.error || original === null || !this.hasBody()) {
      return null;
    }
    return diffJson(original, parsed.value);
  });

  protected readonly changeCount = computed(() => {
    const diff = this.diff();
    return diff ? diff.changes.size + diff.removed.length : 0;
  });

  protected readonly statusChanged = computed(() => this.state().statusCode !== this.state().detail.statusCode);

  protected setView(view: EditorView): void {
    this.view.set(view);
    this.editingPath.set(null);
    this.addingPath.set(null);
  }

  protected toggleCollapse(path: string): void {
    const next = new Set(this.collapsed());
    if (!next.delete(path)) {
      next.add(path);
    }
    this.collapsed.set(next);
  }

  protected rowChange(row: IJsonTreeRow): JsonChange | null {
    return this.diff()?.changes.get(row.path) ?? null;
  }

  protected originalValue(row: IJsonTreeRow): string {
    const value = this.diff()?.originalValues.get(row.path);
    return value === undefined ? '' : literalText(value);
  }

  protected valueClass(value: unknown): string {
    if (typeof value === 'string') {
      return 'text-emerald-700';
    }
    if (typeof value === 'number') {
      return 'text-blue-700';
    }
    if (typeof value === 'boolean') {
      return 'text-violet-700';
    }
    return 'text-secondary italic';
  }

  protected display(value: unknown): string {
    return typeof value === 'string' ? `"${value}"` : JSON.stringify(value);
  }

  protected startEdit(row: IJsonTreeRow): void {
    this.addingPath.set(null);
    this.editingPath.set(row.path);
  }

  /** Commits an inline value edit. A string stays a string; other types parse the typed literal. */
  protected commitEdit(row: IJsonTreeRow, text: string): void {
    if (this.editingPath() !== row.path) {
      return;
    }
    this.editingPath.set(null);
    const value = typeof row.value === 'string' ? text : parseLiteral(text);
    if (JSON.stringify(value) === JSON.stringify(row.value)) {
      return;
    }
    this.emitValue(setAt(this.parsed().value, row.segments, value));
  }

  protected cancelEdit(): void {
    this.editingPath.set(null);
  }

  protected remove(row: IJsonTreeRow): void {
    this.emitValue(removeAt(this.parsed().value, row.segments));
  }

  protected startAdd(row: IJsonTreeRow): void {
    this.editingPath.set(null);
    if (row.kind === 'array') {
      // Arrays: append a copy of the last item (or null) — then edit it in place.
      const items = row.value as unknown[];
      const template = items.length ? structuredClone(items[items.length - 1]) : null;
      this.emitValue(setAt(this.parsed().value, [...row.segments, items.length], template));
      return;
    }
    this.addingPath.set(row.path);
  }

  protected commitAdd(row: IJsonTreeRow, key: string, valueText: string): void {
    const name = key.trim();
    if (!name) {
      return;
    }
    this.addingPath.set(null);
    this.emitValue(setAt(this.parsed().value, [...row.segments, name], parseLiteral(valueText)));
  }

  protected cancelAdd(): void {
    this.addingPath.set(null);
  }

  protected onCodeInput(text: string): void {
    this.localCode.set({ requestId: this.state().detail.requestId, text });
    this.edit.emit({ json: text });
  }

  protected format(): void {
    const parsed = tryParseJson(this.codeText());
    if (parsed.error || !this.codeText().trim()) {
      return;
    }
    const pretty = JSON.stringify(parsed.value, null, 2);
    this.localCode.set({ requestId: this.state().detail.requestId, text: pretty });
    this.edit.emit({ json: pretty });
  }

  private sameJson(a: string, b: string): boolean {
    const left = tryParseJson(a);
    const right = tryParseJson(b);
    return !left.error && !right.error && JSON.stringify(left.value) === JSON.stringify(right.value);
  }

  /** Starts an expected body for a request whose original call failed (no body captured). */
  protected startEmptyBody(): void {
    // A failed capture has no status code — assume success for the expected response.
    this.edit.emit({ json: '{\n  \n}', statusCode: this.state().statusCode ?? 200 });
    this.view.set('code');
  }

  protected onStatusInput(text: string): void {
    const trimmed = text.trim();
    this.edit.emit({ statusCode: trimmed === '' ? null : Number(trimmed) });
  }

  protected onReasonInput(text: string): void {
    this.edit.emit({ reason: text });
  }

  private emitValue(value: unknown): void {
    this.edit.emit({ json: JSON.stringify(value, null, 2) });
  }
}
