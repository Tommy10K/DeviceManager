import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';

import { Device, DeviceType } from '../../../core/models/device.model';
import { DeviceService } from '../../../core/services/device.service';
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

  readonly currentUserRole: 'Admin' | 'User' = 'Admin';

  constructor(
    private readonly deviceService: DeviceService,
    private readonly dialog: MatDialog,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadDevices();
  }

  get isAdmin(): boolean {
    return this.currentUserRole === 'Admin';
  }

  loadDevices(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.deviceService.getAll().subscribe({
      next: (devices) => {
        this.devices = devices;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load devices.';
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
}
