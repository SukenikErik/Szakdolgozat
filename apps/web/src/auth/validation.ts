import type { FieldErrors } from '../api/client'

export function validateEmail(email: string): string[] {
  if (!email.trim()) return ['Enter your email address.']
  return /^[^\s@]+@[^\s@]+$/.test(email.trim()) ? [] : ['Enter a valid email address.']
}

// Keep in sync with the Identity password options in the backend's Program.cs.
export function validateNewPassword(password: string): string[] {
  const valid =
    password.length >= 8 && /[a-z]/.test(password) && /[A-Z]/.test(password) && /[0-9]/.test(password)
  return valid ? [] : ['Use at least 8 characters with a lowercase letter, an uppercase letter and a number.']
}

export const hasErrors = (errors: FieldErrors) => Object.values(errors).some((list) => list.length > 0)
