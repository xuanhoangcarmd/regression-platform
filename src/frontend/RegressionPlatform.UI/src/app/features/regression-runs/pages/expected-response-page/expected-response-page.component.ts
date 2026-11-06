import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';

import { ButtonComponent, ISelectOption, ModalComponent, SelectComponent } from '@shared/components';

import { ExpectedJsonEditorComponent, ExpectedEditPatch } from '../../components/expected-json-editor/expected-json-editor.component';
import { BulkFieldAction } from '../../models/expected-response.model';
import { ExpectedRequestFilter, ExpectedResponseStore, IBulkPreviewItem } from '../../store/expected-response.store';

interface IFilterOption {
  value: ExpectedRequestFilter;
  label: string;
}

/**
 * Expected Response editor for one run: pick an API → pick request(s) → edit the response
 * (tree / code). Edits stay as drafts until "Save all"; a later compare can use the saved
 * expected version instead of the original captured body.
 */
@Component({
  selector: 'app-expected-response-page',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, MatIconModule, ButtonComponent, ModalComponent, SelectComponent, ExpectedJsonEditorComponent],
  providers: [ExpectedResponseStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './expected-response-page.component.html',
})
export class ExpectedResponsePageComponent {
  private readonly route = inject(ActivatedRoute);
  protected readonly store = inject(ExpectedResponseStore);

  protected readonly runId = Number(this.route.snapshot.paramMap.get('runId'));

  protected readonly filterOptions: IFilterOption[] = [
    { value: 'all', label: 'All' },
    { value: 'edited', label: 'Edited' },
    { value: 'draft', label: 'Unsaved' },
  ];

  /** API combobox (value = apiId as string); kept in sync with the store's selection. */
  protected readonly apiControl = new FormControl<string>('', { nonNullable: true });

  /** One option per API with edited / unsaved counts in the label. */
  protected readonly apiOptions = computed<ISelectOption[]>(() => {
    const drafts = this.store.draftCountByApi();
    return this.store.apis().map((api) => {
      const unsaved = drafts.get(api.apiId);
      return {
        value: String(api.apiId),
        label: `${api.apiName} (${api.editedCount}/${api.requestCount} edited${unsaved ? ` · ${unsaved} unsaved` : ''})`,
      };
    });
  });

  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5];

  // Review & save modal.
  protected readonly reviewOpen = signal(false);
  protected readonly commonReason = signal('');

  // Bulk field modal.
  protected readonly bulkOpen = signal(false);
  protected readonly bulkPath = signal('$.DataItems[*].Value');
  protected readonly bulkAction = signal<BulkFieldAction>('set');
  protected readonly bulkValue = signal('');
  protected readonly bulkPreview = signal<IBulkPreviewItem[] | null>(null);
  protected readonly bulkBusy = signal(false);
  protected readonly bulkError = signal<string | null>(null);

  // Restore-original confirmation (ids to restore).
  protected readonly restoreIds = signal<number[] | null>(null);

  protected readonly checkedCount = computed(() => this.store.checked().size);
  protected readonly bulkMatched = computed(() => (this.bulkPreview() ?? []).filter((item) => item.count > 0).length);
  protected readonly bulkTotal = computed(() => (this.bulkPreview() ?? []).reduce((sum, item) => sum + item.count, 0));
  /** Checked requests that have a saved expected version (can be restored). */
  protected readonly checkedEdited = computed(() => {
    const checked = this.store.checked();
    return this.store.visibleRequests().filter((request) => checked.has(request.requestId) && request.isEdited);
  });

  protected readonly checkedEditedIds = computed(() => this.checkedEdited().map((request) => request.requestId));

  constructor() {
    this.store.init(this.runId);

    this.apiControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      if (value) {
        this.store.selectApi(Number(value));
      }
    });
    // Store picks the first API on load → reflect it in the combobox (no echo back).
    toObservable(this.store.selectedApiId)
      .pipe(takeUntilDestroyed())
      .subscribe((apiId) => this.apiControl.setValue(apiId === null ? '' : String(apiId), { emitEvent: false }));
  }

  /** Browser close / reload with unsaved drafts → native "leave site?" prompt. */
  @HostListener('window:beforeunload', ['$event'])
  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.store.pendingCount() > 0) {
      event.preventDefault();
    }
  }

  /** Route guard hook: confirm leaving with unsaved drafts. */
  canLeave(): boolean {
    return this.store.pendingCount() === 0 || window.confirm(`You have ${this.store.pendingCount()} unsaved change(s). Leave and discard them?`);
  }

  protected onEdit(patch: ExpectedEditPatch): void {
    this.store.editActive(patch);
  }

  protected openReview(): void {
    this.commonReason.set('');
    this.reviewOpen.set(true);
  }

  protected async saveAll(): Promise<void> {
    if (await this.store.saveAll(this.commonReason())) {
      this.reviewOpen.set(false);
    }
  }

  protected openFromReview(requestId: number): void {
    this.reviewOpen.set(false);
    this.store.openRequest(requestId);
  }

  protected openBulk(): void {
    this.bulkPreview.set(null);
    this.bulkError.set(null);
    this.bulkOpen.set(true);
  }

  protected onBulkChange(): void {
    // Any input change invalidates the previous preview.
    this.bulkPreview.set(null);
    this.bulkError.set(null);
  }

  protected async previewBulk(): Promise<void> {
    if (!this.bulkPath().trim()) {
      this.bulkError.set('Enter a JSON path.');
      return;
    }
    this.bulkBusy.set(true);
    try {
      const preview = await this.store.previewBulk([...this.store.checked()], this.bulkPath(), this.bulkAction(), this.bulkValue());
      this.bulkPreview.set(preview);
      const pathError = preview.find((item) => item.error && item.error !== 'No response body')?.error ?? null;
      this.bulkError.set(pathError);
    } finally {
      this.bulkBusy.set(false);
    }
  }

  protected async applyBulk(): Promise<void> {
    if (!this.bulkPreview()) {
      await this.previewBulk();
      return;
    }
    this.bulkBusy.set(true);
    try {
      await this.store.applyBulk([...this.store.checked()], this.bulkPath(), this.bulkAction(), this.bulkValue());
      this.bulkOpen.set(false);
    } finally {
      this.bulkBusy.set(false);
    }
  }

  protected askRestore(ids: number[]): void {
    this.restoreIds.set(ids);
  }

  protected async confirmRestore(): Promise<void> {
    const ids = this.restoreIds();
    if (ids?.length) {
      await this.store.restoreOriginal(ids);
    }
    this.restoreIds.set(null);
  }

  protected methodClass(method: string): string {
    switch (method.toUpperCase()) {
      case 'GET':
        return 'bg-blue-50 text-blue-700';
      case 'POST':
        return 'bg-green-50 text-green-700';
      case 'PUT':
        return 'bg-amber-50 text-amber-700';
      case 'DELETE':
        return 'bg-red-50 text-red-700';
      default:
        return 'bg-surface-container-high text-on-surface';
    }
  }
}
