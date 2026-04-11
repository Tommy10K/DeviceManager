import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';

interface LoginRequest {
  email: string;
  password: string;
}

interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  location: string;
}

interface AuthResponse {
  token: string;
  email: string;
  role: string;
}

interface CurrentUser {
  id: string;
  name: string;
  email: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'device_manager_token';
  private readonly authApiUrl = `${environment.apiUrl}/auth`;

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.authApiUrl}/login`, request)
      .pipe(tap((response) => this.setToken(response.token)));
  }

  register(request: RegisterRequest): Observable<unknown> {
    return this.http.post(`${this.authApiUrl}/register`, request);
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    const payload = this.decodeTokenPayload(token);
    const exp = Number(payload?.['exp']);
    if (!Number.isFinite(exp)) {
      return false;
    }

    return exp > Date.now() / 1000;
  }

  getCurrentUser(): CurrentUser | null {
    const token = this.getToken();
    if (!token) {
      return null;
    }

    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return null;
    }

    const id = payload['sub'] as string | undefined;
    const name =
      (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string | undefined) ??
      (payload['name'] as string | undefined);
    const email =
      (payload['email'] as string | undefined) ??
      (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] as string | undefined);
    const role =
      (payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as string | undefined) ??
      (payload['role'] as string | undefined);

    if (!id || !name || !email || !role) {
      return null;
    }

    return { id, name, email, role };
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'Admin';
  }

  private setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  private decodeTokenPayload(token: string): Record<string, unknown> | null {
    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }

    try {
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const normalized = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const payload = atob(normalized);
      return JSON.parse(payload) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}
