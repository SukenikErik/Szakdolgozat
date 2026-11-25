import { useEffect, useState, type ReactNode } from 'react'
import { ApiError } from '../api/client'
import type { AuthResponse, ConfirmEmailRequest } from '../api/types'
import { AuthContext, type AuthContextValue, type AuthState } from './AuthContext'
import { api, clearToken, getToken, saveToken, SESSION_EXPIRED_EVENT } from './session'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => ({ status: getToken() ? 'loading' : 'guest' }))

  function logout() {
    clearToken()
    setState({ status: 'guest' })
  }

  useEffect(() => {
    function handleSessionExpired() {
      clearToken()
      setState({ status: 'guest' })
    }

    window.addEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
  }, [])

  // Restore the session after a page reload.
  useEffect(() => {
    if (!getToken()) return
    let cancelled = false

    api
      .me()
      .then((user) => {
        if (!cancelled) setState({ status: 'authenticated', user })
      })
      .catch((error: unknown) => {
        // A rejected token is handled by the session expired event above.
        const rejected = error instanceof ApiError && error.status === 401
        if (!cancelled && !rejected) setState({ status: 'unreachable' })
      })

    return () => {
      cancelled = true
    }
  }, [])

  function startSession({ accessToken, user }: AuthResponse) {
    saveToken(accessToken)
    setState({ status: 'authenticated', user })
  }

  async function confirmEmail(data: ConfirmEmailRequest) {
    await api.confirmEmail(data)
    if (!getToken()) return

    // Reload the user, so the warning banner disappears. The confirmation is valid even if this fails.
    const user = await api.me().catch(() => null)
    if (user) setState({ status: 'authenticated', user })
  }

  const value: AuthContextValue = {
    state,
    login: async (data) => startSession(await api.login(data)),
    register: async (data) => startSession(await api.register(data)),
    confirmEmail,
    logout,
  }

  return <AuthContext value={value}>{children}</AuthContext>
}
