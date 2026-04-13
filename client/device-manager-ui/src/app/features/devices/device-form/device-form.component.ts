import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil, TimeoutError, timeout } from 'rxjs';

import {
  CreateDeviceRequest,
  Device,
  DeviceType,
  GenerateDescriptionRequest,
} from '../../../core/models/device.model';
import { AuthService } from '../../../core/services/auth.service';
import { DeviceService } from '../../../core/services/device.service';
import { ErrorStateService } from '../../../core/services/error-state.service';
import { getApiErrorMessage } from '../../../core/utils/api-error-message';

@Component({
  selector: 'app-device-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
  ],
  templateUrl: './device-form.component.html',
  styleUrl: './device-form.component.scss',
})
export class DeviceFormComponent implements OnInit, OnDestroy {
  isEditMode = false;
  isLoading = false;
  isSubmitting = false;
  isGeneratingDescription = false;
  errorMessage = '';
  loadErrorMessage = '';
  private deviceId: string | null = null;
  private readonly destroy$ = new Subject<void>();
  private loadingWatchdog: ReturnType<typeof setTimeout> | null = null;
  readonly form;

  readonly deviceTypes = [
    { label: 'Phone', value: DeviceType.Phone },
    { label: 'Tablet', value: DeviceType.Tablet },
  ];

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly authService: AuthService,
    private readonly deviceService: DeviceService,
    private readonly errorStateService: ErrorStateService,
  ) {
    this.form = this.formBuilder.nonNullable.group({
      tag: ['', [Validators.required, Validators.maxLength(100)]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      manufacturer: ['', [Validators.required, Validators.maxLength(100)]],
      type: [DeviceType.Phone, [Validators.required]],
      operatingSystem: ['', [Validators.required, Validators.maxLength(50)]],
      osVersion: ['', [Validators.required, Validators.maxLength(50)]],
      processor: ['', [Validators.required, Validators.maxLength(100)]],
      ramAmount: ['', [Validators.required, Validators.maxLength(50)]],
      description: [''],
      assignedUserId: [''],
    });
  }

  ngOnInit(): void {
    this.errorStateService.clearApiError();

    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe((paramMap) => {
        const id = paramMap.get('id');

        if (!id) {
          this.isEditMode = false;
          this.deviceId = null;
          this.isLoading = false;
          this.clearLoadingWatchdog();
          return;
        }

        this.isEditMode = true;
        this.deviceId = id;

        if (this.tryApplyNavigationDeviceState(id)) {
          this.isLoading = false;
          this.loadErrorMessage = '';
          return;
        }

        this.loadDeviceForEdit(id);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearLoadingWatchdog();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Device' : 'Add Device';
  }

  save(): void {
    if (this.form.invalid || !this.isAdmin) {
      this.form.markAllAsTouched();
      return;
    }

    this.loadErrorMessage = '';
    this.errorMessage = '';
    this.isSubmitting = true;
    this.errorStateService.clearApiError();
    const request = this.buildRequest();

    if (this.isEditMode && this.deviceId) {
      this.deviceService.update(this.deviceId, request).subscribe({
        next: () => {
          this.snackBar.open('Device updated successfully.', 'Close', {
            duration: 2500,
          });
          this.router.navigate(['/devices']);
        },
        error: (error: unknown) => this.handleSaveError(error),
        complete: () => {
          this.isSubmitting = false;
        },
      });

      return;
    }

    this.deviceService.create(request).subscribe({
      next: () => {
        this.snackBar.open('Device created successfully.', 'Close', {
          duration: 2500,
        });
        this.router.navigate(['/devices']);
      },
      error: (error: unknown) => this.handleSaveError(error),
      complete: () => {
        this.isSubmitting = false;
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/devices']);
  }

  generateDescription(): void {
    if (!this.isAdmin) {
      return;
    }

    if (!this.areDescriptionInputsValid()) {
      this.errorMessage = 'Fill in all technical fields before generating a description.';
      this.errorStateService.setApiError(this.errorMessage);
      return;
    }

    this.errorMessage = '';
    this.isGeneratingDescription = true;
    this.errorStateService.clearApiError();

    const request = this.buildGenerateDescriptionRequest();

    this.deviceService.generateDescription(request).subscribe({
      next: (description) => {
        const generatedText = this.normalizeGeneratedDescription(
          typeof description === 'string' ? description : String(description),
        );

        if (generatedText.length === 0) {
          this.errorMessage = 'AI provider returned an empty description.';
          this.errorStateService.setApiError(this.errorMessage);
          this.snackBar.open(this.errorMessage, 'Close', {
            duration: 3000,
          });

          return;
        }

        this.form.controls.description.setValue(generatedText);
        this.form.controls.description.markAsDirty();
        this.isGeneratingDescription = false;

        this.snackBar.open('Description generated.', 'Close', {
          duration: 2500,
        });
      },
      error: (error: unknown) => {
        this.errorMessage = getApiErrorMessage(error, 'Failed to generate description.');
        this.errorStateService.setApiError(this.errorMessage);
        this.isGeneratingDescription = false;

        this.snackBar.open(this.errorMessage, 'Close', {
          duration: 3000,
        });
      },
      complete: () => {
        this.isGeneratingDescription = false;
      },
    });
  }

  private loadDeviceForEdit(id: string): void {
    this.isLoading = true;
    this.loadErrorMessage = '';
    this.errorStateService.clearApiError();
    this.startLoadingWatchdog();

    this.deviceService.getById(id).pipe(
      timeout(15000),
      takeUntil(this.destroy$),
    ).subscribe({
      next: (device) => {
        try {
          this.applyDeviceToForm(device);
        } catch {
          this.loadErrorMessage = 'Device data could not be prepared for editing.';
          this.errorStateService.setApiError(this.loadErrorMessage);
        } finally {
          this.isLoading = false;
          this.clearLoadingWatchdog();
        }
      },
      error: (error: unknown) => {
        if (error instanceof TimeoutError) {
          this.loadErrorMessage = 'Timed out while loading device details. Please try again.';
        } else {
          this.loadErrorMessage = getApiErrorMessage(error, 'Failed to load device.');
        }

        this.errorStateService.setApiError(this.loadErrorMessage);
        this.isLoading = false;
        this.clearLoadingWatchdog();
      },
    });
  }

  private tryApplyNavigationDeviceState(routeId: string): boolean {
    const navigationState =
      this.router.getCurrentNavigation()?.extras.state ?? history.state;

    const candidate = navigationState?.['device'] as Partial<Device> | undefined;
    if (!candidate || typeof candidate !== 'object') {
      return false;
    }

    const candidateId = this.asText(candidate.id);
    if (!candidateId || candidateId !== routeId) {
      return false;
    }

    this.applyDeviceToForm(candidate as Device);
    return true;
  }

  private startLoadingWatchdog(): void {
    this.clearLoadingWatchdog();
    this.loadingWatchdog = setTimeout(() => {
      if (!this.isLoading) {
        return;
      }

      this.loadErrorMessage = 'Loading edit form took too long. Please try again.';
      this.errorStateService.setApiError(this.loadErrorMessage);
      this.isLoading = false;
    }, 20000);
  }

  private clearLoadingWatchdog(): void {
    if (!this.loadingWatchdog) {
      return;
    }

    clearTimeout(this.loadingWatchdog);
    this.loadingWatchdog = null;
  }

  private applyDeviceToForm(device: Device): void {
    this.form.patchValue({
      tag: this.asText(device.tag),
      name: this.asText(device.name),
      manufacturer: this.asText(device.manufacturer),
      type: this.parseDeviceType(device.type),
      operatingSystem: this.asText(device.operatingSystem),
      osVersion: this.asText(device.osVersion),
      processor: this.asText(device.processor),
      ramAmount: this.asText(device.ramAmount),
      description: this.asText(device.description),
      assignedUserId: this.asText(device.assignedUserId),
    });
  }

  private parseDeviceType(value: unknown): DeviceType {
    if (value === DeviceType.Tablet || value === 1 || value === '1') {
      return DeviceType.Tablet;
    }

    if (typeof value === 'string' && value.trim().toLowerCase() === 'tablet') {
      return DeviceType.Tablet;
    }

    return DeviceType.Phone;
  }

  private asText(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }

    if (typeof value === 'string') {
      return value;
    }

    return String(value);
  }

  private buildRequest(): CreateDeviceRequest {
    const formValue = this.form.getRawValue();

    return {
      tag: formValue.tag.trim(),
      name: formValue.name.trim(),
      manufacturer: formValue.manufacturer.trim(),
      type: formValue.type,
      operatingSystem: formValue.operatingSystem.trim(),
      osVersion: formValue.osVersion.trim(),
      processor: formValue.processor.trim(),
      ramAmount: formValue.ramAmount.trim(),
      description: formValue.description.trim() || null,
      assignedUserId: formValue.assignedUserId.trim() || null,
    };
  }

  private areDescriptionInputsValid(): boolean {
    const controls = this.form.controls;

    controls.name.markAsTouched();
    controls.manufacturer.markAsTouched();
    controls.operatingSystem.markAsTouched();
    controls.type.markAsTouched();
    controls.ramAmount.markAsTouched();
    controls.processor.markAsTouched();

    return (
      controls.name.valid &&
      controls.manufacturer.valid &&
      controls.operatingSystem.valid &&
      controls.type.valid &&
      controls.ramAmount.valid &&
      controls.processor.valid
    );
  }

  private buildGenerateDescriptionRequest(): GenerateDescriptionRequest {
    const formValue = this.form.getRawValue();

    const typeLabel = formValue.type === DeviceType.Phone ? 'Phone' : 'Tablet';

    return {
      name: formValue.name.trim(),
      manufacturer: formValue.manufacturer.trim(),
      operatingSystem: formValue.operatingSystem.trim(),
      type: typeLabel,
      ramAmount: formValue.ramAmount.trim(),
      processor: formValue.processor.trim(),
    };
  }

  private handleSaveError(error: unknown): void {
    const httpError = error as HttpErrorResponse;
    this.isSubmitting = false;

    if (httpError.status === 409) {
      this.errorMessage = 'A device with this tag already exists.';
    } else {
      this.errorMessage = getApiErrorMessage(error, 'Failed to save device.');
    }

    this.errorStateService.setApiError(this.errorMessage);

    this.snackBar.open(this.errorMessage, 'Close', {
      duration: 3000,
    });
  }

  private normalizeGeneratedDescription(value: string): string {
    const trimmed = value.trim();

    if (trimmed.startsWith('"') && trimmed.endsWith('"')) {
      try {
        const parsed = JSON.parse(trimmed) as unknown;
        if (typeof parsed === 'string') {
          return parsed.trim();
        }
      } catch {
        // Keep original trimmed value when it is not valid JSON string.
      }
    }

    return trimmed;
  }
}
