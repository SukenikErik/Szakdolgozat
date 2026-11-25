// No React or browser storage here, so this file can move to packages/shared for the mobile app.
import type { AuthResponse, ConfirmEmailRequest, LoginRequest, RegisterRequest, User } from './types'

export type FieldErrors = Record<string, string[]>

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  // Status 0 means that the server could not be reached.
  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

interface ClientOptions {
  baseUrl: string
  getToken: () => string | null
  /** Called when the server rejects the stored token (for example because it expired). */
  onUnauthorized: () => void
}

interface ProblemDetails {
  detail?: string
  errors?: FieldErrors
}

export function createApiClient({ baseUrl, getToken, onUnauthorized }: ClientOptions) {
  async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const token = getToken()
    const headers: Record<string, string> = {}
    if (token) headers.Authorization = `Bearer ${token}`
    if (body !== undefined) headers['Content-Type'] = 'application/json'

    let response: Response
    try {
      response = await fetch(baseUrl + path, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
      })
    } catch {
      throw new ApiError(0, 'Could not reach the server. Please try again.')
    }

    if (!response.ok) {
      if (response.status === 401 && token) onUnauthorized()
      throw await toApiError(response)
    }
    // 204 No Content has no body.
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  return {
    register: (data: RegisterRequest) => request<AuthResponse>('POST', '/api/auth/register', data),
    login: (data: LoginRequest) => request<AuthResponse>('POST', '/api/auth/login', data),
    confirmEmail: (data: ConfirmEmailRequest) => request<void>('POST', '/api/auth/confirm-email', data),
    resendConfirmation: () => request<void>('POST', '/api/auth/resend-confirmation'),
    me: () => request<User>('GET', '/api/auth/me'),
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  // The body may be empty or not JSON (for example a proxy error).
  const problem: ProblemDetails = await response.json().catch(() => ({}))

  // The server sends C# property names ("Email"), the forms use camelCase ("email").
  const fieldErrors: FieldErrors = {}
  for (const [name, messages] of Object.entries(problem.errors ?? {})) {
    fieldErrors[name.charAt(0).toLowerCase() + name.slice(1)] = messages
  }

  const message = problem.detail ?? 'Something went wrong. Please try again.'
  return new ApiError(response.status, message, fieldErrors)
}
