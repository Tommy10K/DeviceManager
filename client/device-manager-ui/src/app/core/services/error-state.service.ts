import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ErrorStateService {
  private readonly apiErrorSubject = new BehaviorSubject<string>('');

  readonly apiError$ = this.apiErrorSubject.asObservable();

  setApiError(message: string): void {
    this.apiErrorSubject.next(message);
  }

  clearApiError(): void {
    this.apiErrorSubject.next('');
  }
}
