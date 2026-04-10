import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';

import { Device, DeviceType } from '../../../core/models/device.model';
import { DeviceService } from '../../../core/services/device.service';

@Component({
  selector: 'app-device-detail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './device-detail.component.html',
  styleUrl: './device-detail.component.scss',
})
export class DeviceDetailComponent implements OnInit {
  device: Device | null = null;
  isLoading = true;
  notFound = false;
  errorMessage = '';

  readonly currentUserRole: 'Admin' | 'User' = 'Admin';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly deviceService: DeviceService,
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
    return this.currentUserRole === 'Admin';
  }

  get typeLabel(): string {
    if (!this.device) {
      return '';
    }

    return this.device.type === DeviceType.Phone ? 'Phone' : 'Tablet';
  }

  goBack(): void {
    this.router.navigate(['/devices']);
  }

  goToEdit(): void {
    if (!this.device) {
      return;
    }

    this.router.navigate(['/devices', this.device.id, 'edit']);
  }

  private loadDevice(deviceId: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.deviceService.getById(deviceId).subscribe({
      next: (device) => {
        this.device = device;
        this.isLoading = false;
      },
      error: (error: unknown) => {
        const httpError = error as HttpErrorResponse;

        if (httpError.status === 404) {
          this.notFound = true;
        } else {
          this.errorMessage = 'Failed to load device details.';
        }

        this.isLoading = false;
      },
    });
  }
}
