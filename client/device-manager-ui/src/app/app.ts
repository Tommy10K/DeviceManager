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
  readonly currentUserRole: 'Admin' | 'User' = 'Admin';
  pageTitle = 'Devices';

  constructor(
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly titleService: Title,
    private readonly errorStateService: ErrorStateService,
  ) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updatePageTitle();
      });

    this.updatePageTitle();
  }

  get isAdmin(): boolean {
    return this.currentUserRole === 'Admin';
  }

  get apiError$() {
    return this.errorStateService.apiError$;
  }

  dismissApiError(): void {
    this.errorStateService.clearApiError();
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
