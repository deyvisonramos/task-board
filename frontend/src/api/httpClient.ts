import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import {
  clearTokens,
  getAccessToken,
  getRefreshToken,
  saveTokens,
} from '../auth/authTokenStorage'

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

type RefreshResponse = {
  accessToken: string
  refreshToken: string
}

export type ObservableApiError = AxiosError<ApiErrorResponse> & {
  correlationId?: string
  requestId?: string
  traceId?: string
}

type RetriableRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean
}

httpClient.interceptors.response.use(
  (response) => response,
  async (error: ObservableApiError) => {
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

    const originalRequest = error.config as RetriableRequestConfig | undefined
    const refreshToken = getRefreshToken()

    if (
      error.response?.status === 401 &&
      refreshToken &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/api/auth/refresh')
    ) {
      originalRequest._retry = true

      try {
        const refreshResponse = await axios.post<RefreshResponse>(
          `${API_BASE_URL}/api/auth/refresh`,
          { refreshToken },
        )

        saveTokens({
          accessToken: refreshResponse.data.accessToken,
          refreshToken: refreshResponse.data.refreshToken,
        })
        originalRequest.headers.Authorization = `Bearer ${refreshResponse.data.accessToken}`

        return httpClient(originalRequest)
      } catch {
        clearTokens()
      }
    }

    return Promise.reject(error)
  },
)
