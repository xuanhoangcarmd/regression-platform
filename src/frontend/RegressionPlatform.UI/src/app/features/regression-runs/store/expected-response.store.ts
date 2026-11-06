import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, filter, firstValueFrom, of, switchMap, tap } from 'rxjs';

import { ExpectedResponseApiService } from '../apis/expected-response-api.service';
import { applyBulkAction, parseLiteral, tryParseJson } from '../helpers/json-edit.helper';
import {
  BulkFieldAction,
  IExpectedApi,
  IExpectedDraft,
  IExpectedRequestItem,
  IExpectedResponseDetail,
  IExpectedSaveItem,
} from '../models/expected-response.model';

export type ExpectedRequestFilter = 'all' | 'edited' | 'draft';

/** The editable state of the active request (its draft, or the saved/original body when untouched). */
export interface IExpectedEditState {
  detail: IExpectedResponseDetail;
  json: string;
  statusCode: number | null;
  reason: string;
  isDraft: boolean;
}

/** A pending (unsaved) draft with display info for the top bar / review list. */
export interface IExpectedPendingItem {
  draft: IExpectedDraft;
  title: string;
  apiName: string;
  jsonValid: boolean;
}

/** Bulk action preview: how many places change per request. */
export interface IBulkPreviewItem {
  requestId: number;
  title: string;
  count: number;
  error: string | null;
}

/** Rows rendered per "Show more" step (an API can have thousands of requests). */
const RENDER_STEP = 300;

/** ProblemDetails / validation errors → one readable line. */
function errorText(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | null;
    const validation = body?.errors ? Object.values(body.errors).flat().join(' ') : '';
    return validation || body?.detail || body?.title || fallback;
  }
  return error instanceof Error ? error.message : fallback;
}

export interface IExpectedBanner {
  tone: 'success' | 'error';
  text: string;
}

/**
 * Page-scoped store of the Expected Response editor: API / request selection, the response cache,
 * and the client-side drafts (many requests can be edited, then saved together in one batch).
 * Provided by the page component, so state is reset when the page is left.
 */
@Injectable()
export class ExpectedResponseStore {
  private readonly api = inject(ExpectedResponseApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly runIdState = signal(0);
  private readonly apisState = signal<IExpectedApi[]>([]);
  private readonly apisLoadingState = signal(true);
  private readonly selectedApiIdState = signal<number | null>(null);
  private readonly requestsState = signal<IExpectedRequestItem[]>([]);
  private readonly requestsLoadingState = signal(false);
  private readonly searchState = signal('');
  private readonly filterState = signal<ExpectedRequestFilter>('all');
  private readonly checkedState = signal<ReadonlySet<number>>(new Set());
  private readonly activeRequestIdState = signal<number | null>(null);
  private readonly detailLoadingState = signal(false);
  private readonly detailsState = signal<ReadonlyMap<number, IExpectedResponseDetail>>(new Map());
  private readonly draftsState = signal<ReadonlyMap<number, IExpectedDraft>>(new Map());
  private readonly savingState = signal(false);
  private readonly bannerState = signal<IExpectedBanner | null>(null);
  private readonly renderLimitState = signal(RENDER_STEP);

  readonly runId = this.runIdState.asReadonly();
  readonly apis = this.apisState.asReadonly();
  readonly apisLoading = this.apisLoadingState.asReadonly();
  readonly selectedApiId = this.selectedApiIdState.asReadonly();
  readonly requestsLoading = this.requestsLoadingState.asReadonly();
  readonly search = this.searchState.asReadonly();
  readonly filter = this.filterState.asReadonly();
  readonly checked = this.checkedState.asReadonly();
  readonly activeRequestId = this.activeRequestIdState.asReadonly();
  readonly detailLoading = this.detailLoadingState.asReadonly();
  readonly drafts = this.draftsState.asReadonly();
  readonly saving = this.savingState.asReadonly();
  readonly banner = this.bannerState.asReadonly();

  readonly selectedApi = computed(() => this.apisState().find((api) => api.apiId === this.selectedApiIdState()) ?? null);

  /** The first N visible requests (rendered); the rest appear with "Show more". */
  readonly renderedRequests = computed(() => this.visibleRequests().slice(0, this.renderLimitState()));
  readonly hiddenCount = computed(() => Math.max(0, this.visibleRequests().length - this.renderLimitState()));

  /** Requests of the selected API after search + filter (natural order by title). */
  readonly visibleRequests = computed(() => {
    const term = this.searchState().trim().toLowerCase();
    const mode = this.filterState();
    const drafts = this.draftsState();
    return this.requestsState().filter((request) => {
      const matchesTerm =
        !term ||
        request.title.toLowerCase().includes(term) ||
        (request.description ?? '').toLowerCase().includes(term);
      const matchesMode =
        mode === 'all' || (mode === 'edited' ? request.isEdited : drafts.has(request.requestId));
      return matchesTerm && matchesMode;
    });
  });

  readonly allVisibleChecked = computed(() => {
    const visible = this.visibleRequests();
    const checked = this.checkedState();
    return visible.length > 0 && visible.every((request) => checked.has(request.requestId));
  });

  /** Draft count per API (badge in the API list). */
  readonly draftCountByApi = computed(() => {
    const counts = new Map<number, number>();
    const details = this.detailsState();
    for (const draft of this.draftsState().values()) {
      const apiId = details.get(draft.requestId)?.apiId;
      if (apiId !== undefined) {
        counts.set(apiId, (counts.get(apiId) ?? 0) + 1);
      }
    }
    return counts;
  });

  readonly pending = computed<IExpectedPendingItem[]>(() => {
    const details = this.detailsState();
    const apis = this.apisState();
    return [...this.draftsState().values()].map((draft) => {
      const detail = details.get(draft.requestId);
      return {
        draft,
        title: detail?.title ?? `#${draft.requestId}`,
        apiName: apis.find((api) => api.apiId === detail?.apiId)?.apiName ?? '',
        jsonValid: tryParseJson(draft.json).error === null,
      };
    });
  });

  readonly pendingCount = computed(() => this.draftsState().size);
  readonly hasInvalidDraft = computed(() => this.pending().some((item) => !item.jsonValid));

  /** The active request's editable state (draft if any, else saved expected, else original). */
  readonly active = computed<IExpectedEditState | null>(() => {
    const id = this.activeRequestIdState();
    const detail = id === null ? undefined : this.detailsState().get(id);
    if (!detail) {
      return null;
    }
    const draft = this.draftsState().get(detail.requestId);
    if (draft) {
      return { detail, json: draft.json, statusCode: draft.statusCode, reason: draft.reason, isDraft: true };
    }
    const base = this.baseOf(detail);
    return { detail, ...base, isDraft: false };
  });

  constructor() {
    // Reload the request list when the API changes — switchMap drops a stale API's response.
    toObservable(this.selectedApiIdState)
      .pipe(
        filter((apiId): apiId is number => apiId !== null),
        tap(() => this.requestsLoadingState.set(true)),
        switchMap((apiId) => this.api.getRequests(this.runIdState(), apiId).pipe(catchError(() => of([])))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((requests) => {
        this.requestsState.set(this.sortRequests(requests));
        this.requestsLoadingState.set(false);
      });
  }

  /** Loads the run's APIs and selects the first one. */
  init(runId: number): void {
    this.runIdState.set(runId);
    this.loadApis(true);
  }

  selectApi(apiId: number): void {
    if (apiId === this.selectedApiIdState()) {
      return;
    }
    this.selectedApiIdState.set(apiId);
    this.checkedState.set(new Set());
    this.renderLimitState.set(RENDER_STEP);
  }

  setSearch(term: string): void {
    this.searchState.set(term);
    this.renderLimitState.set(RENDER_STEP);
  }

  setFilter(mode: ExpectedRequestFilter): void {
    this.filterState.set(mode);
    this.renderLimitState.set(RENDER_STEP);
  }

  showMore(): void {
    this.renderLimitState.update((limit) => limit + RENDER_STEP);
  }

  toggleChecked(requestId: number): void {
    const next = new Set(this.checkedState());
    if (!next.delete(requestId)) {
      next.add(requestId);
    }
    this.checkedState.set(next);
  }

  toggleAllVisible(): void {
    const ids = this.visibleRequests().map((request) => request.requestId);
    const next = new Set(this.checkedState());
    if (this.allVisibleChecked()) {
      ids.forEach((id) => next.delete(id));
    } else {
      ids.forEach((id) => next.add(id));
    }
    this.checkedState.set(next);
  }

  clearChecked(): void {
    this.checkedState.set(new Set());
  }

  /** Opens a request in the editor (loads its bodies once). */
  openRequest(requestId: number): void {
    this.activeRequestIdState.set(requestId);
    if (this.detailsState().has(requestId)) {
      return;
    }
    this.detailLoadingState.set(true);
    this.api
      .getDetails(this.runIdState(), [requestId])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (details) => {
          this.cacheDetails(details);
          this.detailLoadingState.set(false);
        },
        error: (error: unknown) => {
          this.detailLoadingState.set(false);
          this.showBanner('error', errorText(error, 'Failed to load the response.'));
        },
      });
  }

  /** Updates the active request's draft; a draft equal to the saved/original body is dropped. */
  editActive(patch: Partial<Pick<IExpectedDraft, 'json' | 'statusCode' | 'reason'>>): void {
    const state = this.active();
    if (!state) {
      return;
    }
    const next: IExpectedDraft = {
      requestId: state.detail.requestId,
      json: patch.json ?? state.json,
      statusCode: patch.statusCode !== undefined ? patch.statusCode : state.statusCode,
      reason: patch.reason ?? state.reason,
    };
    this.putDraft(next);
  }

  discardDraft(requestId: number): void {
    const next = new Map(this.draftsState());
    next.delete(requestId);
    this.draftsState.set(next);
  }

  discardAll(): void {
    this.draftsState.set(new Map());
  }

  /** Saves every draft in one batch. `commonReason` fills drafts without their own reason. */
  async saveAll(commonReason: string): Promise<boolean> {
    const drafts = [...this.draftsState().values()];
    if (!drafts.length || this.hasInvalidDraft()) {
      return false;
    }
    const items: IExpectedSaveItem[] = drafts.map((draft) => ({
      requestId: draft.requestId,
      statusCode: draft.statusCode,
      // Store compact JSON; formatting is a view concern.
      responseJson: JSON.stringify(JSON.parse(draft.json)),
      reason: (draft.reason.trim() || commonReason.trim()) || null,
    }));
    this.savingState.set(true);
    try {
      const saved = await firstValueFrom(this.api.save(this.runIdState(), items));
      await this.refreshAfterWrite(items.map((item) => item.requestId));
      this.draftsState.set(new Map());
      this.showBanner('success', `Saved ${saved} expected response(s).`);
      return true;
    } catch (error) {
      this.showBanner('error', errorText(error, 'Save failed.'));
      return false;
    } finally {
      this.savingState.set(false);
    }
  }

  /** Deletes the saved expected versions → these requests compare against the original again. */
  async restoreOriginal(requestIds: number[]): Promise<void> {
    this.savingState.set(true);
    try {
      const removed = await firstValueFrom(this.api.restoreOriginal(this.runIdState(), requestIds));
      const drafts = new Map(this.draftsState());
      requestIds.forEach((id) => drafts.delete(id));
      this.draftsState.set(drafts);
      await this.refreshAfterWrite(requestIds);
      this.showBanner('success', `Restored ${removed} response(s) to the original.`);
    } catch (error) {
      this.showBanner('error', errorText(error, 'Restore failed.'));
    } finally {
      this.savingState.set(false);
    }
  }

  /** Dry run of a bulk field action over the given requests (bodies loaded as needed). */
  async previewBulk(requestIds: number[], path: string, action: BulkFieldAction, valueText: string): Promise<IBulkPreviewItem[]> {
    try {
      await this.ensureDetails(requestIds);
    } catch (error) {
      this.showBanner('error', errorText(error, 'Failed to load the responses.'));
      return [];
    }
    return requestIds.map((requestId) => {
      const outcome = this.bulkOutcome(requestId, path, action, valueText);
      return { requestId, title: this.detailsState().get(requestId)?.title ?? `#${requestId}`, count: outcome.count, error: outcome.error };
    });
  }

  /** Applies a bulk field action into the drafts (nothing is saved until Save all). */
  async applyBulk(requestIds: number[], path: string, action: BulkFieldAction, valueText: string): Promise<number> {
    try {
      await this.ensureDetails(requestIds);
    } catch (error) {
      this.showBanner('error', errorText(error, 'Failed to load the responses.'));
      return 0;
    }
    let touched = 0;
    for (const requestId of requestIds) {
      const outcome = this.bulkOutcome(requestId, path, action, valueText);
      if (outcome.error || outcome.count === 0 || outcome.json === null) {
        continue;
      }
      const current = this.currentOf(requestId);
      if (!current) {
        continue;
      }
      this.putDraft({ requestId, json: outcome.json, statusCode: current.statusCode, reason: current.reason });
      touched++;
    }
    this.showBanner('success', `Applied to ${touched} request(s) as drafts — review, then Save all.`);
    return touched;
  }

  dismissBanner(): void {
    this.bannerState.set(null);
  }

  /** Saved expected body/status if any, else the original. */
  baseOf(detail: IExpectedResponseDetail): { json: string; statusCode: number | null; reason: string } {
    if (detail.override) {
      return {
        json: this.pretty(detail.override.responseJson),
        statusCode: detail.override.statusCode ?? detail.statusCode,
        reason: detail.override.reason ?? '',
      };
    }
    return { json: detail.responseJson ? this.pretty(detail.responseJson) : '', statusCode: detail.statusCode, reason: '' };
  }

  // ---------------- internals ----------------

  private loadApis(selectFirst: boolean): void {
    this.apisLoadingState.set(true);
    this.api
      .getApis(this.runIdState())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (apis) => {
          this.apisState.set(apis);
          this.apisLoadingState.set(false);
          if (selectFirst && apis.length && this.selectedApiIdState() === null) {
            this.selectApi(apis[0].apiId);
          }
        },
        error: (error: unknown) => {
          this.apisLoadingState.set(false);
          this.showBanner('error', errorText(error, 'Failed to load APIs.'));
        },
      });
  }

  /** After save/restore: refresh API counts, the request list, and the touched bodies. */
  private async refreshAfterWrite(requestIds: number[]): Promise<void> {
    const runId = this.runIdState();
    const [apis, details] = await Promise.all([
      firstValueFrom(this.api.getApis(runId)),
      firstValueFrom(this.api.getDetails(runId, requestIds)),
    ]);
    this.apisState.set(apis);
    this.cacheDetails(details);
    const apiId = this.selectedApiIdState();
    if (apiId !== null) {
      this.requestsState.set(this.sortRequests(await firstValueFrom(this.api.getRequests(runId, apiId))));
    }
  }

  private async ensureDetails(requestIds: number[]): Promise<void> {
    const missing = requestIds.filter((id) => !this.detailsState().has(id));
    if (missing.length) {
      this.cacheDetails(await firstValueFrom(this.api.getDetails(this.runIdState(), missing)));
    }
  }

  private cacheDetails(details: IExpectedResponseDetail[]): void {
    const next = new Map(this.detailsState());
    details.forEach((detail) => next.set(detail.requestId, detail));
    this.detailsState.set(next);
  }

  /** Current editable state of any request (draft, else saved/original). */
  private currentOf(requestId: number): { json: string; statusCode: number | null; reason: string } | null {
    const draft = this.draftsState().get(requestId);
    if (draft) {
      return draft;
    }
    const detail = this.detailsState().get(requestId);
    return detail ? this.baseOf(detail) : null;
  }

  private bulkOutcome(
    requestId: number,
    path: string,
    action: BulkFieldAction,
    valueText: string,
  ): { count: number; json: string | null; error: string | null } {
    const current = this.currentOf(requestId);
    if (!current || !current.json.trim()) {
      return { count: 0, json: null, error: 'No response body' };
    }
    const parsed = tryParseJson(current.json);
    if (parsed.error) {
      return { count: 0, json: null, error: 'Invalid JSON in draft' };
    }
    try {
      const { result, count } = applyBulkAction(parsed.value, path, action, parseLiteral(valueText));
      return { count, json: JSON.stringify(result, null, 2), error: null };
    } catch (error) {
      return { count: 0, json: null, error: error instanceof Error ? error.message : 'Invalid path' };
    }
  }

  /** Natural order by title (TC_2 before TC_10), request id as tiebreaker. */
  private sortRequests(requests: IExpectedRequestItem[]): IExpectedRequestItem[] {
    return [...requests].sort(
      (a, b) => a.title.localeCompare(b.title, undefined, { numeric: true, sensitivity: 'base' }) || a.requestId - b.requestId,
    );
  }

  private putDraft(draft: IExpectedDraft): void {
    const detail = this.detailsState().get(draft.requestId);
    const next = new Map(this.draftsState());
    if (detail && this.sameAsBase(draft, detail)) {
      next.delete(draft.requestId);
    } else {
      next.set(draft.requestId, draft);
    }
    this.draftsState.set(next);
  }

  private sameAsBase(draft: IExpectedDraft, detail: IExpectedResponseDetail): boolean {
    const base = this.baseOf(detail);
    return (
      draft.statusCode === base.statusCode &&
      draft.reason.trim() === base.reason.trim() &&
      this.normalize(draft.json) === this.normalize(base.json)
    );
  }

  /** Compact form for equality (falls back to raw text while the JSON is invalid). */
  private normalize(json: string): string {
    const parsed = tryParseJson(json);
    return parsed.error ? json : JSON.stringify(parsed.value);
  }

  private pretty(json: string): string {
    const parsed = tryParseJson(json);
    return parsed.error ? json : JSON.stringify(parsed.value, null, 2);
  }

  private showBanner(tone: IExpectedBanner['tone'], text: string): void {
    this.bannerState.set({ tone, text });
  }
}
