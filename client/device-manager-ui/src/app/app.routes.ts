import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DeviceDetailComponent } from './features/devices/device-detail/device-detail.component';
import { DeviceFormComponent } from './features/devices/device-form/device-form.component';
import { DeviceListComponent } from './features/devices/device-list/device-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'devices', pathMatch: 'full' },
  { path: 'login', component: LoginComponent, data: { title: 'Login' } },
  { path: 'register', component: RegisterComponent, data: { title: 'Register' } },
  { path: 'devices', component: DeviceListComponent, canActivate: [authGuard], data: { title: 'Devices' } },
  {
    path: 'devices/new',
    component: DeviceFormComponent,
    canActivate: [authGuard, adminGuard],
    data: { title: 'Add Device' },
  },
  {
    path: 'devices/:id/edit',
    component: DeviceFormComponent,
    canActivate: [authGuard, adminGuard],
    data: { title: 'Edit Device' },
  },
  { path: 'devices/:id', component: DeviceDetailComponent, canActivate: [authGuard], data: { title: 'Device Details' } },
  { path: '**', redirectTo: 'devices' },
];
