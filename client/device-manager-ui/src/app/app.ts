import { Component } from '@angular/core';
import { AsyncPipe, NgIf } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Title } from '@angular/platform-browser';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from './core/services/auth.service';
import { ErrorStateService } from './core/services/error-state.service';

@Component({
  selector: 'app-root',
  imports: [
    AsyncPipe,
    NgIf,
    MatButtonModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  pageTitle = 'Devices';

  constructor(
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly titleService: Title,
    private readonly authService: AuthService,
    private readonly errorStateService: ErrorStateService,
  ) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updatePageTitle();
      });

    this.updatePageTitle();
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  get currentUserName(): string {
    return this.authService.getCurrentUser()?.name ?? '';
  }

  get apiError$() {
    return this.errorStateService.apiError$;
  }

  dismissApiError(): void {
    this.errorStateService.clearApiError();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private updatePageTitle(): void {
    let route = this.activatedRoute;

    while (route.firstChild) {
      route = route.firstChild;
    }

    const routeTitle = (route.snapshot.data['title'] as string | undefined) ?? 'Device Manager';
    this.pageTitle = routeTitle;
    this.titleService.setTitle(`${routeTitle} | Device Manager`);
  }
}
