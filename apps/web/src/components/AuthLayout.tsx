import type { ReactNode } from 'react'
import styles from './AuthLayout.module.css'

interface AuthLayoutProps {
  title: string
  subtitle: string
  children: ReactNode
}

/** The common frame of the login and sign up screens. */
export function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
  return (
    <div className={styles.page}>
      <div className={styles.rings} aria-hidden="true" />

      <header className={styles.header}>
        <span className={styles.logo}>
          balance<span className={styles.dot}>.</span>
        </span>
        <span className={styles.tagline}>Make room for what matters.</span>
      </header>

      <main className={styles.main}>
        <section className={styles.hero}>
          <p className={styles.eyebrow}>Less worry. More possibility.</p>
          <p className={styles.headline}>
            Your money.
            <br />A clearer future.
          </p>
          <p className={styles.description}>
            Build a budget that fits your life. Track your spending, grow your savings, and feel good
            about what’s next.
          </p>
          <p className={styles.closing}>Small steps today. More freedom tomorrow.</p>
        </section>

        <section className={styles.card}>
          <h1>{title}</h1>
          <p className={styles.subtitle}>{subtitle}</p>
          {children}
        </section>
      </main>

      <footer className={styles.footer}>© 2026 Balance</footer>
    </div>
  )
}
