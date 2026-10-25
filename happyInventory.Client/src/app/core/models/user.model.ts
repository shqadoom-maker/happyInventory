export type UserRole = 'Admin' | 'Management' | 'Auditor';

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: UserRole;
  isActive: boolean;
  createdUtc: string;
  lastLoginUtc?: string | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresUtc: string;
  user: User;
}

export interface CreateUserRequest {
  username: string;
  fullName: string;
  role: UserRole;
  password: string;
  isActive: boolean;
}

export interface UpdateUserRequest {
  username: string;
  fullName: string;
  role: UserRole;
  password?: string | null;
  isActive: boolean;
}

export interface ChangePasswordRequest {
  newPassword: string;
}
