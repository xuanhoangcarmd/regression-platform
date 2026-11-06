import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';

import { AppIconsConstants } from '@core/constants';
import { ButtonComponent, InputComponent, ToggleComponent } from '@shared/components';
import { controlErrorMessage } from '@shared/utils';

import { ProjectConfigurationApiService } from '../../apis/project-configuration-api.service';
import {
  EnvironmentKey,
  EnvironmentStateMap,
  IEnvironmentConfigurationResponse,
  IEnvironmentState,
  IHeaderState,
  ISaveEnvironmentConfiguration,
} from '../../models/environment-config.model';

type HeaderForm = FormGroup<{
  key: FormControl<string>;
  value: FormControl<string>;
  secret: FormControl<boolean>;
  /** UI-only: false = value is masked (stored secret); true = plaintext, editable & visible. */
  editing: FormControl<boolean>;
  /** UI-only: a value is stored on the server (drives masking for secrets). */
  hasValue: FormControl<boolean>;
}>;

interface IEnvironmentTab {
  key: EnvironmentKey;
  label: string;
}

/** Per-project environment configuration: Base URL + request headers, backed by the API. */
@Component({
  selector: 'app-configurations-page',
  standalone: true,
  imports: [ReactiveFormsModule, MatIconModule, ButtonComponent, InputComponent, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './configurations-page.component.html',
})
export class ConfigurationsPageComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly api = inject(ProjectConfigurationApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly controlErrorMessage = controlErrorMessage;

  protected readonly deleteIcon = AppIconsConstants.delete;
  protected readonly addIcon = AppIconsConstants.add;

  // http(s) URL; header names use the RFC 7230 token charset (no spaces).
  private static readonly urlPattern = /^https?:\/\/.+/i;
  private static readonly headerKeyPattern = /^[A-Za-z0-9!#$%&'*+.^_`|~-]+$/;

  protected readonly tabs: IEnvironmentTab[] = [
    { key: 'DEV', label: 'DEV' },
    { key: 'STG', label: 'STG' },
    { key: 'PROD', label: 'PROD' },
  ];

  private readonly projectCode = this.resolveProjectCode();

  protected readonly activeEnv = signal<EnvironmentKey>('DEV');
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly store = signal<EnvironmentStateMap>(ConfigurationsPageComponent.emptyState());

  protected readonly form = this.formBuilder.group({
    baseUrl: this.formBuilder.control('', [
      Validators.required,
      Validators.maxLength(500),
      Validators.pattern(ConfigurationsPageComponent.urlPattern),
    ]),
    headers: this.formBuilder.array<HeaderForm>([]),
  });

  protected readonly activeLabel = computed(() => this.tabs.find((tab) => tab.key === this.activeEnv())?.label ?? '');

  constructor() {
    this.fetchConfigurations();
  }

  protected get headers(): FormArray<HeaderForm> {
    return this.form.controls.headers;
  }

  protected selectEnv(env: EnvironmentKey): void {
    if (env === this.activeEnv()) {
      return;
    }
    // Keep the current working copy so switching tabs doesn't lose edits.
    this.persistForm();
    this.activeEnv.set(env);
    this.saved.set(false);
    this.loadForm();
  }

  protected addHeader(): void {
    this.headers.push(this.headerGroup({ key: '', value: '', secret: false, hasValue: false, editing: true }));
    this.saved.set(false);
  }

  protected removeHeader(index: number): void {
    this.headers.removeAt(index);
    this.saved.set(false);
  }

  /** A stored secret with a value is masked (hidden) until the user chooses to change it. */
  protected isMasked(group: HeaderForm): boolean {
    return group.controls.secret.value && !group.controls.editing.value && group.controls.hasValue.value;
  }

  /** "Change": unlock a masked secret to enter a new value — clears the old one so it's never revealed. */
  protected changeSecret(group: HeaderForm): void {
    group.controls.value.setValue('');
    group.controls.editing.setValue(true);
    // Now visible/editable — a value is required again.
    group.controls.value.setValidators([Validators.required, Validators.maxLength(2000)]);
    group.controls.value.updateValueAndValidity();
    this.saved.set(false);
  }

  protected save(): void {
    if (!this.projectCode) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.persistForm();
    this.saving.set(true);
    this.errorMessage.set(null);

    const environmentKey = this.activeEnv();
    this.api
      .save(this.projectCode, environmentKey, this.buildPayload(this.store()[environmentKey]))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saved.set(true);
          // Re-fetch so masked/hasValue reflects what is actually stored.
          this.fetchConfigurations();
        },
        error: () => {
          this.saving.set(false);
          this.errorMessage.set('Failed to save configuration. Please try again.');
        },
      });
  }

  protected cancel(): void {
    // Discard working edits for the active environment (revert to the last loaded snapshot).
    this.loadForm();
    this.saved.set(false);
  }

  private fetchConfigurations(): void {
    if (!this.projectCode) {
      this.errorMessage.set('Unable to determine the project.');
      return;
    }
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getConfigurations(this.projectCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (configurations) => {
          this.loading.set(false);
          this.store.set(this.toStateMap(configurations));
          this.loadForm();
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('Failed to load configuration. Please try again.');
        },
      });
  }

  /** Loads the active environment's stored state into the form. */
  private loadForm(): void {
    const state = this.store()[this.activeEnv()];
    this.form.controls.baseUrl.setValue(state.baseUrl);
    this.form.setControl('headers', this.formBuilder.array(state.headers.map((header) => this.headerGroup(header))));
  }

  /** Writes the form's current working state back into the in-memory store for the active environment. */
  private persistForm(): void {
    const value = this.form.getRawValue();
    const state: IEnvironmentState = {
      baseUrl: value.baseUrl,
      headers: value.headers.map((header) => ({
        key: header.key,
        value: header.value,
        secret: header.secret,
        hasValue: header.hasValue,
        editing: header.editing,
      })),
    };
    this.store.update((map) => ({ ...map, [this.activeEnv()]: state }));
  }

  /** Builds the save payload from a working state, applying the secret rules. */
  private buildPayload(state: IEnvironmentState): ISaveEnvironmentConfiguration {
    return {
      baseUrl: state.baseUrl.trim() || null,
      headers: state.headers
        .filter((header) => header.key.trim().length > 0)
        .map((header) => {
          if (!header.secret) {
            return { key: header.key.trim(), value: header.value, isSecret: false, valueProvided: true };
          }
          // Secret: send a new value only when the user was editing it; otherwise keep the stored one.
          const valueProvided = header.editing;
          return {
            key: header.key.trim(),
            value: valueProvided ? header.value : null,
            isSecret: true,
            valueProvided,
          };
        }),
    };
  }

  private toStateMap(configurations: IEnvironmentConfigurationResponse[]): EnvironmentStateMap {
    const map = ConfigurationsPageComponent.emptyState();
    for (const configuration of configurations) {
      map[configuration.environment] = {
        baseUrl: configuration.baseUrl ?? '',
        headers: configuration.headers.map((header) => ({
          key: header.key,
          value: header.isSecret ? '' : (header.value ?? ''),
          secret: header.isSecret,
          hasValue: header.hasValue,
          // Stored secrets start masked; everything else starts editable.
          editing: !(header.isSecret && header.hasValue),
        })),
      };
    }
    return map;
  }

  private headerGroup(header: IHeaderState): HeaderForm {
    // A masked stored secret needs no value entered; everything visible requires a value.
    const masked = header.secret && !header.editing && header.hasValue;
    return this.formBuilder.group({
      key: this.formBuilder.control(header.key, [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(ConfigurationsPageComponent.headerKeyPattern),
      ]),
      value: this.formBuilder.control(
        header.value,
        masked ? [Validators.maxLength(2000)] : [Validators.required, Validators.maxLength(2000)],
      ),
      secret: this.formBuilder.control(header.secret),
      editing: this.formBuilder.control(header.editing),
      hasValue: this.formBuilder.control(header.hasValue),
    });
  }

  /** Walks up the route tree to find the `:projectId` param (the project code/slug). */
  private resolveProjectCode(): string {
    let route: ActivatedRoute | null = this.route;
    while (route) {
      const code = route.snapshot.paramMap.get('projectId');
      if (code) {
        return code;
      }
      route = route.parent;
    }
    return '';
  }

  private static emptyState(): EnvironmentStateMap {
    return {
      DEV: { baseUrl: '', headers: [] },
      STG: { baseUrl: '', headers: [] },
      PROD: { baseUrl: '', headers: [] },
    };
  }
}
