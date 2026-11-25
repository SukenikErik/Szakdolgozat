import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router'
import { useAuth } from './auth/AuthContext'
import { ConfirmEmailBanner } from './components/ConfirmEmailBanner'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { SignUpPage } from './pages/SignUpPage'

function App() {
  const { state, logout } = useAuth()

  if (state.status === 'loading') return <p className="status">Loading…</p>

  if (state.status === 'unreachable') {
    return (
      <div className="status" role="alert">
        <p>Could not reach the server.</p>
        <button type="button" onClick={() => window.location.reload()}>
          Try again
        </button>
        <button type="button" onClick={logout}>
          Log out
        </button>
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route element={<GuestOnly />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignUpPage />} />
        </Route>
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        <Route element={<RequireAuth />}>
          <Route path="/" element={<HomePage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

function RequireAuth() {
  const { state } = useAuth()
  if (state.status !== 'authenticated') return <Navigate to="/login" replace />

  return (
    <>
      <ConfirmEmailBanner />
      <Outlet />
    </>
  )
}

function GuestOnly() {
  const { state } = useAuth()
  return state.status === 'authenticated' ? <Navigate to="/" replace /> : <Outlet />
}

export default App
