import { User } from './user.model';

export enum DeviceType {
  Phone = 0,
  Tablet = 1,
}

export interface Device {
  id: string;
  tag: string;
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmount: string;
  description: string | null;
  assignedUserId: string | null;
  assignedUser: User | null;
}

export interface CreateDeviceRequest {
  tag: string;
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmount: string;
  description: string | null;
  assignedUserId: string | null;
}

export type UpdateDeviceRequest = CreateDeviceRequest;