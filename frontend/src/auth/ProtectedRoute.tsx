import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './useAuth'

export function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return (
      <section className="page" aria-labelledby="loading-title">
        <div className="page-panel">
          <p className="page-kicker">Loading</p>
          <h1 id="loading-title" className="page-title">
            Checking your session
          </h1>
          <p className="page-description">
            Hang tight while TaskBoard confirms your sign-in.
          </p>
        </div>
      </section>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <Outlet />
}
