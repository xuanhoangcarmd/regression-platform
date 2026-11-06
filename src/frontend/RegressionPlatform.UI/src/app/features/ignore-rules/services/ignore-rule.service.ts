import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, map, Observable, of, switchMap, tap } from 'rxjs';

import { ProjectContextService } from '@core/services';
import { ApiDefinitionApiService } from '@features/api-definitions/apis/api-definition-api.service';
import { ISelectOption } from '@shared/components';

import { IgnoreRuleApiService } from '../apis/ignore-rule-api.service';
import { IIgnoreRule, IIgnoreRuleWrite } from '../models/ignore-rule.model';

/** Owns the Ignore Rules state, backed by the API (`/api/ignore-rules`), scoped to the active project. */
@Injectable({ providedIn: 'root' })
export class IgnoreRuleService {
  private readonly api = inject(IgnoreRuleApiService);
  private readonly apiDefinitionApi = inject(ApiDefinitionApiService);
  private readonly projectContext = inject(ProjectContextService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly rulesState = signal<IIgnoreRule[]>([]);
  readonly rules = this.rulesState.asReadonly();
  readonly count = computed(() => this.rulesState().length);
  readonly loading = signal(false);

  /** API options (id → name) for the "Applies To" multi-select, scoped to the active project. */
  readonly apiOptions = signal<ISelectOption[]>([]);

  // Bumped to force a re-fetch after a create/update/delete.
  private readonly refreshTrigger = signal(0);

  private readonly query = computed(() => ({
    project: this.projectContext.code(),
    refresh: this.refreshTrigger(),
  }));

  constructor() {
    // Rules — re-fetch when the project changes or after a mutation. switchMap cancels any
    // in-flight request, so a stale response can never overwrite a newer one.
    toObservable(this.query)
      .pipe(
        switchMap((query) => {
          if (!query.project) {
            return of<IIgnoreRule[]>([]);
          }
          this.loading.set(true);
          return this.api.getRules().pipe(catchError(() => of<IIgnoreRule[]>([])));
        }),
        tap(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((rules) => this.rulesState.set(rules));

    // API options for the multi-select — reload whenever the active project changes.
    toObservable(computed(() => this.projectContext.code()))
      .pipe(
        switchMap((projectCode) =>
          projectCode
            ? this.apiDefinitionApi.getApis({ pageNumber: 1, pageSize: 100, isActive: true }).pipe(
                map((page) => page.items.map((api) => ({ value: String(api.id), label: api.name }))),
                catchError(() => of<ISelectOption[]>([])),
              )
            : of<ISelectOption[]>([]),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((options) => this.apiOptions.set(options));
  }

  /** Creates a rule and refreshes the list; returns the call so callers can await completion. */
  create(payload: IIgnoreRuleWrite): Observable<IIgnoreRule> {
    return this.api.create(payload).pipe(tap(() => this.refresh()));
  }

  /** Updates a rule and refreshes the list; returns the call so callers can await completion. */
  update(id: number, payload: IIgnoreRuleWrite): Observable<void> {
    return this.api.update(id, payload).pipe(tap(() => this.refresh()));
  }

  /** Deletes a rule and refreshes the list; returns the call so callers can await completion. */
  remove(id: number): Observable<void> {
    return this.api.delete(id).pipe(tap(() => this.refresh()));
  }

  private refresh(): void {
    this.refreshTrigger.update((value) => value + 1);
  }
}

/** Formats the numeric id as the display id shown in the table (e.g. RUL-001). */
export function displayRuleId(id: number): string {
  return `RUL-${String(id).padStart(3, '0')}`;
}
