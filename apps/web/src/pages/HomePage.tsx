import { useAuth } from '../auth/AuthContext'
import styles from './HomePage.module.css'

/** Temporary home page, the dashboard replaces it later. */
export function HomePage() {
  const { state, logout } = useAuth()
  if (state.status !== 'authenticated') return null

  return (
    <main className={styles.page}>
      <h1>Hello, {state.user.displayName ?? state.user.email}!</h1>
      <p>Your dashboard will appear here.</p>
      <button type="button" onClick={logout}>
        Log out
      </button>
    </main>
  )
}
