import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

import { Device, DeviceType } from '../../../core/models/device.model';
import { AuthService } from '../../../core/services/auth.service';
import { DeviceService } from '../../../core/services/device.service';
import { ErrorStateService } from '../../../core/services/error-state.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-device-list',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './device-list.component.html',
  styleUrl: './device-list.component.scss',
})
export class DeviceListComponent implements OnInit {
  devices: Device[] = [];
  isLoading = true;
  errorMessage = '';

  readonly displayedColumns: string[] = [
    'tag',
    'name',
    'manufacturer',
    'type',
    'operatingSystem',
    'assignedUser',
    'actions',
  ];

  constructor(
    private readonly authService: AuthService,
    private readonly deviceService: DeviceService,
    private readonly errorStateService: ErrorStateService,
    private readonly dialog: MatDialog,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get currentUserId(): string | null {
    return this.authService.getCurrentUser()?.id ?? null;
  }

  loadDevices(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.errorStateService.clearApiError();

    this.deviceService.getAll().subscribe({
      next: (devices) => {
        this.devices = devices;
        this.isLoading = false;
        this.errorStateService.clearApiError();
      },
      error: () => {
        this.errorMessage = 'Failed to load devices.';
        this.errorStateService.setApiError(this.errorMessage);
        this.isLoading = false;
      },
    });
  }

  openCreateForm(): void {
    this.router.navigate(['/devices/new']);
  }

  openDetails(deviceId: string): void {
    this.router.navigate(['/devices', deviceId]);
  }

  confirmDelete(device: Device, event: Event): void {
    if (!this.isAdmin) {
      return;
    }

    event.stopPropagation();

    const dialogData: ConfirmDialogData = {
      title: 'Delete device',
      message: `Are you sure you want to delete ${device.tag}?`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
    };

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '360px',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean | undefined) => {
      if (!confirmed) {
        return;
      }

      this.deviceService.delete(device.id).subscribe({
        next: () => this.loadDevices(),
        error: () => {
          this.errorMessage = 'Failed to delete device.';
          this.errorStateService.setApiError(this.errorMessage);
        },
      });
    });
  }

  getTypeLabel(type: DeviceType): string {
    return type === DeviceType.Phone ? 'Phone' : 'Tablet';
  }

  getAssignedUserLabel(device: Device): string {
    return device.assignedUser?.name ?? 'Unassigned';
  }

  canAssign(device: Device): boolean {
    return !this.isAdmin && !device.assignedUserId;
  }

  canUnassign(device: Device): boolean {
    return !this.isAdmin && !!this.currentUserId && device.assignedUserId === this.currentUserId;
  }

  isAssignedToOtherUser(device: Device): boolean {
    return !!device.assignedUserId && device.assignedUserId !== this.currentUserId;
  }

  assignToMe(device: Device, event: Event): void {
    event.stopPropagation();

    this.deviceService.assign(device.id).subscribe({
      next: () => this.loadDevices(),
      error: () => {
        this.errorMessage = 'Failed to assign device.';
        this.errorStateService.setApiError(this.errorMessage);
      },
    });
  }

  unassign(device: Device, event: Event): void {
    event.stopPropagation();

    this.deviceService.unassign(device.id).subscribe({
      next: () => this.loadDevices(),
      error: () => {
        this.errorMessage = 'Failed to unassign device.';
        this.errorStateService.setApiError(this.errorMessage);
      },
    });
  }

  openEdit(deviceId: string, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/devices', deviceId, 'edit']);
  }
}
