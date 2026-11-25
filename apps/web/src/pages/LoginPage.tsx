import { useState, type SubmitEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, type FieldErrors } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { hasErrors, validateEmail } from '../auth/validation'
import { AuthLayout } from '../components/AuthLayout'
import { TextField } from '../components/TextField'
import styles from './AuthForm.module.css'

export function LoginPage() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: SubmitEvent) {
    event.preventDefault()
    setFormError('')

    const found: FieldErrors = {
      email: validateEmail(email),
      password: password ? [] : ['Enter your password.'],
    }
    setErrors(found)
    if (hasErrors(found)) return

    setSubmitting(true)
    try {
      // On success the route guard redirects to the home page.
      await login({ email: email.trim(), password })
    } catch (error) {
      const fieldErrors = error instanceof ApiError ? error.fieldErrors : {}
      setErrors(fieldErrors)
      if (!hasErrors(fieldErrors)) {
        setFormError(error instanceof ApiError ? error.message : 'Something went wrong. Please try again.')
      }
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Welcome back" subtitle="Log in to take the next step toward your goals.">
      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        {formError && (
          <p className={styles.formError} role="alert">
            {formError}
          </p>
        )}
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
          autoComplete="current-password"
          placeholder="Enter your password"
          maxLength={128}
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          errors={errors.password}
        />
        <button type="submit" className={styles.primary} disabled={submitting}>
          {submitting ? 'Logging in…' : 'Log in'}
        </button>
      </form>
      <p className={styles.switch}>New to Balance?</p>
      <Link to="/signup" className={styles.secondary}>
        Sign up
      </Link>
    </AuthLayout>
  )
}
