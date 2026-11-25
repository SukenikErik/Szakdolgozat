import { createContext, useContext } from 'react'
import type { ConfirmEmailRequest, LoginRequest, RegisterRequest, User } from '../api/types'

export type AuthState =
  | { status: 'loading' }
  | { status: 'unreachable' } // there is a saved token, but the server did not answer
  | { status: 'guest' }
  | { status: 'authenticated'; user: User }

export interface AuthContextValue {
  state: AuthState
  login: (data: LoginRequest) => Promise<void>
  register: (data: RegisterRequest) => Promise<void>
  confirmEmail: (data: ConfirmEmailRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
