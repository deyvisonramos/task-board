import { Component, type ErrorInfo, type ReactNode } from 'react'

type ErrorBoundaryProps = {
  children: ReactNode
}

type ErrorBoundaryState = {
  hasError: boolean
  requestId?: string
}

type ErrorWithRequestId = {
  correlationId?: string
  requestId?: string
  traceId?: string
}

export class ErrorBoundary extends Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  public state: ErrorBoundaryState = {
    hasError: false,
  }

  public static getDerivedStateFromError(error: unknown): ErrorBoundaryState {
    return {
      hasError: true,
      requestId: getRequestId(error),
    }
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Unexpected React rendering error.', {
      error,
      componentStack: errorInfo.componentStack,
    })
  }

  public render() {
    if (!this.state.hasError) {
      return this.props.children
    }

    return (
      <section className="page" aria-labelledby="error-boundary-title">
        <div className="page-panel error-boundary">
          <p className="page-kicker">Unexpected error</p>
          <h1 id="error-boundary-title" className="page-title">
            Something went wrong
          </h1>
          <p className="page-description">
            Please retry or contact support with the request ID if available.
          </p>
          {this.state.requestId ? (
            <p className="request-id">Request ID: {this.state.requestId}</p>
          ) : null}
        </div>
      </section>
    )
  }
}

function getRequestId(error: unknown) {
  if (typeof error !== 'object' || error === null) {
    return undefined
  }

  const observedError = error as ErrorWithRequestId

  return (
    observedError.requestId ??
    observedError.correlationId ??
    observedError.traceId
  )
}
