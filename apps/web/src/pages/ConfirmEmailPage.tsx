import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AuthLayout } from '../components/AuthLayout'
import styles from './AuthForm.module.css'

// The user has to press a button, so an email scanner that only opens the link cannot confirm the address.
export function ConfirmEmailPage() {
  const { state, confirmEmail } = useAuth()
  const [params] = useSearchParams()
  const userId = params.get('userId')
  const token = params.get('token')
  const [status, setStatus] = useState<'ready' | 'sending' | 'done'>('ready')
  const [error, setError] = useState('')

  const continueLink = (
    <Link to="/" className={styles.primary}>
      {state.status === 'authenticated' ? 'Continue' : 'Log in'}
    </Link>
  )

  if (!userId || !token) {
    return (
      <AuthLayout title="Invalid link" subtitle="This confirmation link is incomplete. Open it again from the email.">
        <div className={styles.form}>{continueLink}</div>
      </AuthLayout>
    )
  }

  if (status === 'done') {
    return (
      <AuthLayout title="Email confirmed" subtitle="Thank you! Your account is now permanent.">
        <div className={styles.form}>{continueLink}</div>
      </AuthLayout>
    )
  }

  async function confirm(userId: string, token: string) {
    setStatus('sending')
    setError('')
    try {
      await confirmEmail({ userId, token })
      setStatus('done')
    } catch (error) {
      // A damaged user id is rejected with a validation error, which has no readable message.
      const invalid = error instanceof ApiError && error.status === 400
      setError(
        invalid
          ? 'This confirmation link is invalid or has expired.'
          : error instanceof ApiError
            ? error.message
            : 'Something went wrong. Please try again.',
      )
      setStatus('ready')
    }
  }

  return (
    <AuthLayout title="Confirm your email" subtitle="One click, and your account is here to stay.">
      <div className={styles.form}>
        {error && (
          <p className={styles.formError} role="alert">
            {error}
          </p>
        )}
        <button
          type="button"
          className={styles.primary}
          disabled={status === 'sending'}
          onClick={() => confirm(userId, token)}
        >
          {status === 'sending' ? 'Confirming…' : 'Confirm email'}
        </button>
      </div>
    </AuthLayout>
  )
}
