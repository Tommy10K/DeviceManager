import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';

import { Device, DeviceType } from '../../../core/models/device.model';
import { AuthService } from '../../../core/services/auth.service';
import { DeviceService } from '../../../core/services/device.service';
import { ErrorStateService } from '../../../core/services/error-state.service';
import { getApiErrorMessage } from '../../../core/utils/api-error-message';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-device-detail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatChipsModule, MatDialogModule, MatProgressSpinnerModule],
  templateUrl: './device-detail.component.html',
  styleUrl: './device-detail.component.scss',
})
export class DeviceDetailComponent implements OnInit {
  device: Device | null = null;
  isLoading = true;
  isGeneratingDescription = false;
  notFound = false;
  errorMessage = '';
  assignmentError = '';
  generatedDescription = '';
  generatedDescriptionError = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly deviceService: DeviceService,
    private readonly dialog: MatDialog,
    private readonly errorStateService: ErrorStateService,
  ) {}

  ngOnInit(): void {
    const deviceId = this.route.snapshot.paramMap.get('id');

    if (!deviceId) {
      this.notFound = true;
      this.isLoading = false;
      return;
    }

    this.loadDevice(deviceId);
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get typeLabel(): string {
    if (!this.device) {
      return '';
    }

    return this.device.type === DeviceType.Phone ? 'Phone' : 'Tablet';
  }

  get currentUserId(): string | null {
    return this.authService.getCurrentUser()?.id ?? null;
  }

  get assignedUserLabel(): string {
    return this.device?.assignedUser?.name || 'Unassigned';
  }

  get isAssignedToCurrentUser(): boolean {
    return !!this.device?.assignedUserId && this.device.assignedUserId === this.currentUserId;
  }

  get canToggleAssignment(): boolean {
    if (!this.device) {
      return false;
    }

    return !this.device.assignedUserId || this.isAssignedToCurrentUser;
  }

  goToEdit(): void {
    if (!this.device) {
      return;
    }

    this.router.navigate(['/devices', this.device.id, 'edit'], {
      state: { device: this.device },
    });
  }

  handleAssignmentAction(): void {
    if (!this.device || !this.canToggleAssignment) {
      return;
    }

    this.assignmentError = '';

    const isAssignAction = !this.device.assignedUserId;

    const dialogData: ConfirmDialogData = {
      title: isAssignAction ? 'Assign device' : 'Unassign device',
      message: isAssignAction
        ? `Do you want to assign ${this.device.name} to yourself?`
        : `Do you want to unassign ${this.device.name} from yourself?`,
      confirmText: isAssignAction ? 'Assign' : 'Unassign',
      cancelText: 'Cancel',
    };

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean | undefined) => {
      if (!confirmed || !this.device) {
        return;
      }

      const request = isAssignAction
        ? this.deviceService.assign(this.device.id)
        : this.deviceService.unassign(this.device.id);

      request.subscribe({
        next: (updatedDevice) => {
          this.device = updatedDevice;
          this.assignmentError = '';
          this.errorStateService.clearApiError();
        },
        error: (error: unknown) => {
          this.assignmentError = getApiErrorMessage(error, 'Failed to assign device.');
          this.errorStateService.setApiError(this.assignmentError);
        },
      });
    });
  }

  generateDescription(): void {
    if (!this.device) {
      return;
    }

    this.generatedDescriptionError = '';
    this.isGeneratingDescription = true;

    this.deviceService.generateDescriptionForDevice(this.device.id).subscribe({
      next: (description) => {
        const generatedText = this.normalizeGeneratedDescription(
          typeof description === 'string' ? description : String(description),
        );

        if (generatedText.length === 0) {
          this.generatedDescriptionError = 'AI provider returned an empty description.';
          this.generatedDescription = '';
          this.errorStateService.setApiError(this.generatedDescriptionError);
          this.isGeneratingDescription = false;
          return;
        }

        this.generatedDescription = generatedText;
        this.errorStateService.clearApiError();
        this.isGeneratingDescription = false;
      },
      error: (error: unknown) => {
        this.generatedDescriptionError = getApiErrorMessage(error, 'Failed to generate description.');
        this.generatedDescription = '';
        this.errorStateService.setApiError(this.generatedDescriptionError);
        this.isGeneratingDescription = false;
      },
      complete: () => {
        this.isGeneratingDescription = false;
      },
    });
  }

  private loadDevice(deviceId: string): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.errorStateService.clearApiError();

    this.deviceService.getById(deviceId).subscribe({
      next: (device) => {
        this.device = device;
        this.assignmentError = '';
        this.isLoading = false;
        this.errorStateService.clearApiError();
      },
      error: (error: unknown) => {
        const httpError = error as HttpErrorResponse;

        if (httpError.status === 404) {
          this.notFound = true;
        } else {
          this.errorMessage = 'Failed to load device details.';
          this.errorStateService.setApiError(this.errorMessage);
        }

        this.isLoading = false;
      },
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
