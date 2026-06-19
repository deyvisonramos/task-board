import axios, { type AxiosError } from 'axios'
import { getAccessToken } from '../auth/authTokenStorage'

export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5141'

export const httpClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

httpClient.interceptors.request.use((config) => {
  const accessToken = getAccessToken()

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }

  return config
})

type ApiErrorResponse = {
  code?: string
  message?: string
  correlationId?: string
  traceId?: string
  title?: string
  detail?: string
}

export type ObservableApiError = AxiosError<ApiErrorResponse> & {
  correlationId?: string
  requestId?: string
  traceId?: string
}

httpClient.interceptors.response.use(
  (response) => response,
  (error: ObservableApiError) => {
    const responseData = error.response?.data
    const responseCorrelationId =
      error.response?.headers['x-correlation-id']?.toString()
    const requestId =
      responseData?.correlationId ??
      responseData?.traceId ??
      responseCorrelationId

    error.correlationId = responseData?.correlationId ?? responseCorrelationId
    error.traceId = responseData?.traceId
    error.requestId = requestId

    return Promise.reject(error)
  },
)
