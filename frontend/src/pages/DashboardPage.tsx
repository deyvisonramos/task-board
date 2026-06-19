import { useAuth } from '../auth/useAuth'

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <section className="page" aria-labelledby="dashboard-title">
      <div className="page-panel">
        <p className="page-kicker">Tasks</p>
        <h1 id="dashboard-title" className="page-title">
          Dashboard
        </h1>
        <p className="page-description">
          Welcome, {user?.email}. Task list and board behavior will be added in
          a later task-focused slice.
        </p>
      </div>
    </section>
  )
}
