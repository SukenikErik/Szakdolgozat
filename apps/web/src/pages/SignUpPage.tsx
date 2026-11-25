import { useState, type SubmitEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, type FieldErrors } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { hasErrors, validateEmail, validateNewPassword } from '../auth/validation'
import { AuthLayout } from '../components/AuthLayout'
import { TextField } from '../components/TextField'
import styles from './AuthForm.module.css'

export function SignUpPage() {
  const { register } = useAuth()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: SubmitEvent) {
    event.preventDefault()
    setFormError('')

    const found: FieldErrors = {
      email: validateEmail(email),
      password: validateNewPassword(password),
      confirmation: confirmation === password ? [] : ['The passwords do not match.'],
    }
    setErrors(found)
    if (hasErrors(found)) return

    setSubmitting(true)
    try {
      // On success the route guard redirects to the home page.
      await register({ email: email.trim(), password, displayName: displayName.trim() || undefined })
    } catch (error) {
      let fieldErrors: FieldErrors = error instanceof ApiError ? error.fieldErrors : {}
      if (error instanceof ApiError && error.status === 409) fieldErrors = { email: [error.message] }
      setErrors(fieldErrors)
      if (!hasErrors(fieldErrors)) {
        setFormError(error instanceof ApiError ? error.message : 'Something went wrong. Please try again.')
      }
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Create your account" subtitle="Start building a budget that fits your life.">
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {formError && (
          <p className={styles.formError} role="alert">
            {formError}
          </p>
        )}
        <TextField
          label="Name (optional)"
          name="displayName"
          autoComplete="name"
          placeholder="Enter your name"
          maxLength={100}
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          errors={errors.displayName}
        />
        <TextField
          label="Email"
          name="email"
          type="email"
          autoComplete="username"
          placeholder="Enter your email"
          maxLength={256}
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          errors={errors.email}
        />
        <TextField
          label="Password"
          name="password"
          type="password"
          autoComplete="new-password"
          placeholder="Create a password"
          hint="At least 8 characters with a lowercase letter, an uppercase letter and a number."
          maxLength={128}
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          errors={errors.password}
        />
        <TextField
          label="Confirm password"
          name="confirmation"
          type="password"
          autoComplete="new-password"
          placeholder="Repeat your password"
          maxLength={128}
          value={confirmation}
          onChange={(event) => setConfirmation(event.target.value)}
          errors={errors.confirmation}
        />
        <button type="submit" className={styles.primary} disabled={submitting}>
          {submitting ? 'Creating account…' : 'Sign up'}
        </button>
      </form>
      <p className={styles.switch}>Already have an account?</p>
      <Link to="/login" className={styles.secondary}>
        Log in
      </Link>
    </AuthLayout>
  )
}
