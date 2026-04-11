import { Routes } from '@angular/router';
import { DeviceDetailComponent } from './features/devices/device-detail/device-detail.component';
import { DeviceFormComponent } from './features/devices/device-form/device-form.component';
import { DeviceListComponent } from './features/devices/device-list/device-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'devices', pathMatch: 'full' },
  { path: 'devices', component: DeviceListComponent, data: { title: 'Devices' } },
  { path: 'devices/new', component: DeviceFormComponent, data: { title: 'Add Device' } },
  { path: 'devices/:id/edit', component: DeviceFormComponent, data: { title: 'Edit Device' } },
  { path: 'devices/:id', component: DeviceDetailComponent, data: { title: 'Device Details' } },
];
