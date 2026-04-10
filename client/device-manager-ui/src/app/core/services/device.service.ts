import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CreateDeviceRequest,
  Device,
  UpdateDeviceRequest,
} from '../models/device.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private readonly apiUrl = `${environment.apiUrl}/devices`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Device[]> {
    return this.http.get<Device[]>(this.apiUrl);
  }

  getById(id: string): Observable<Device> {
    return this.http.get<Device>(`${this.apiUrl}/${id}`);
  }

  create(device: CreateDeviceRequest): Observable<Device> {
    return this.http.post<Device>(this.apiUrl, device);
  }

  update(id: string, device: UpdateDeviceRequest): Observable<Device> {
    return this.http.put<Device>(`${this.apiUrl}/${id}`, device);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}