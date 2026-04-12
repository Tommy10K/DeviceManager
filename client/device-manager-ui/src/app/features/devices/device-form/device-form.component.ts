import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';

import {
  CreateDeviceRequest,
  Device,
  DeviceType,
  GenerateDescriptionRequest,
} from '../../../core/models/device.model';
import { DeviceService } from '../../../core/services/device.service';
import { ErrorStateService } from '../../../core/services/error-state.service';

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
export class DeviceFormComponent implements OnInit {
  isEditMode = false;
  isLoading = false;
  isSubmitting = false;
  isGeneratingDescription = false;
  errorMessage = '';
  private deviceId: string | null = null;
  readonly form;

  readonly currentUserRole: 'Admin' | 'User' = 'Admin';
  readonly deviceTypes = [
    { label: 'Phone', value: DeviceType.Phone },
    { label: 'Tablet', value: DeviceType.Tablet },
  ];

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
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

    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      return;
    }

    this.isEditMode = true;
    this.deviceId = id;
    this.loadDeviceForEdit(id);
  }

  get isAdmin(): boolean {
    return this.currentUserRole === 'Admin';
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Device' : 'Add Device';
  }

  save(): void {
    if (this.form.invalid || !this.isAdmin) {
      this.form.markAllAsTouched();
      return;
    }

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
        this.form.controls.description.setValue(description);
        this.form.controls.description.markAsDirty();

        this.snackBar.open('Description generated.', 'Close', {
          duration: 2500,
        });
      },
      error: () => {
        this.errorMessage = 'Failed to generate description.';
        this.errorStateService.setApiError(this.errorMessage);

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
    this.errorMessage = '';

    this.deviceService.getById(id).subscribe({
      next: (device) => {
        this.applyDeviceToForm(device);
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load device.';
        this.errorStateService.setApiError(this.errorMessage);
        this.isLoading = false;
      },
    });
  }

  private applyDeviceToForm(device: Device): void {
    this.form.patchValue({
      tag: device.tag,
      name: device.name,
      manufacturer: device.manufacturer,
      type: device.type,
      operatingSystem: device.operatingSystem,
      osVersion: device.osVersion,
      processor: device.processor,
      ramAmount: device.ramAmount,
      description: device.description ?? '',
      assignedUserId: device.assignedUserId ?? '',
    });
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

    if (httpError.status === 409) {
      this.errorMessage = 'A device with this tag already exists.';
    } else {
      this.errorMessage = 'Failed to save device.';
    }

    this.errorStateService.setApiError(this.errorMessage);

    this.snackBar.open(this.errorMessage, 'Close', {
      duration: 3000,
    });
  }
}
