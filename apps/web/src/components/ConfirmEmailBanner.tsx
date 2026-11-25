import { useState } from 'react'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { api } from '../auth/session'
import styles from './ConfirmEmailBanner.module.css'

/** Closing it is not saved on purpose: it appears again at the next login or page load. */
export function ConfirmEmailBanner() {
  const { state } = useAuth()
  const [closed, setClosed] = useState(false)
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState('')

  if (closed || state.status !== 'authenticated' || state.user.emailConfirmed) return null
  const { email, confirmationDaysLeft } = state.user

  async function resend() {
    setSending(true)
    setMessage('')
    try {
      await api.resendConfirmation()
      setMessage(`Email sent to ${email}.`)
    } catch (error) {
      setMessage(error instanceof ApiError ? error.message : 'Something went wrong. Please try again.')
    } finally {
      setSending(false)
    }
  }

  return (
    <aside className={styles.banner} aria-label="Email confirmation">
      <p className={styles.text}>
        Please confirm your email address. <strong>{deadlineText(confirmationDaysLeft ?? 0)}</strong>
      </p>
      <p className={styles.message} role="status">
        {message}
      </p>
      <button type="button" className={styles.resend} onClick={resend} disabled={sending}>
        {sending ? 'Sending…' : 'Resend email'}
      </button>
      <button type="button" className={styles.close} onClick={() => setClosed(true)} aria-label="Close">
        ×
      </button>
    </aside>
  )
}

function deadlineText(days: number): string {
  if (days <= 0) return 'Your account is about to be deleted.'
  return `${days} ${days === 1 ? 'day' : 'days'} left before your account is deleted.`
}
