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
  rememberMe?: boolean;
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
  currentPassword: string;
  newPassword: string;
}

/** Administrator-only password reset; the current password is unknown. */
export interface ResetPasswordRequest {
  newPassword: string;
}
