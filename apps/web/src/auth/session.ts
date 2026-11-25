import { createApiClient } from '../api/client'

const TOKEN_KEY = 'balance.accessToken'

export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const saveToken = (token: string) => localStorage.setItem(TOKEN_KEY, token)
export const clearToken = () => localStorage.removeItem(TOKEN_KEY)

// The AuthProvider listens to this event to log the user out.
export const SESSION_EXPIRED_EVENT = 'balance:session-expired'

export const api = createApiClient({
  baseUrl: import.meta.env.VITE_API_URL ?? '',
  getToken,
  onUnauthorized: () => window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT)),
})
