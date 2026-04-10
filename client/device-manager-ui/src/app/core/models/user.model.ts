export enum UserRole {
  User = 0,
  Admin = 1,
}

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  location: string;
}