import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import {
  CreateDeviceRequest,
  Device,
  GenerateDescriptionRequest,
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

  search(query: string): Observable<Device[]> {
    const params = new HttpParams().set('q', query);
    return this.http.get<Device[]>(`${this.apiUrl}/search`, { params });
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

  assign(id: string): Observable<Device> {
    return this.http.post<Device>(`${this.apiUrl}/${id}/assign`, {});
  }

  unassign(id: string): Observable<Device> {
    return this.http.post<Device>(`${this.apiUrl}/${id}/unassign`, {});
  }

  generateDescription(request: GenerateDescriptionRequest): Observable<string> {
    return this.http
      .post(`${this.apiUrl}/generate-description`, request, {
        observe: 'response',
        responseType: 'text' as const,
      })
      .pipe(map((response) => response.body ?? ''));
  }

  generateDescriptionForDevice(id: string): Observable<string> {
    return this.http
      .post(`${this.apiUrl}/${id}/generate-description`, {}, {
        observe: 'response',
        responseType: 'text' as const,
      })
      .pipe(map((response) => response.body ?? ''));
  }
}