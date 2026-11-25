// Mirrors backend/BudgetApp.Api/Dtos/AuthDtos.cs

export interface RegisterRequest {
  email: string
  password: string
  displayName?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ConfirmEmailRequest {
  userId: string
  token: string
}

export interface User {
  userId: string
  email: string
  displayName: string | null
  createdAt: string
  emailConfirmed: boolean
  /** Days until the unconfirmed account is deleted, null if it is confirmed. */
  confirmationDaysLeft: number | null
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: User
}
