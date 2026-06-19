import { httpClient } from './httpClient'

export type RegisterRequest = {
  email: string
  password: string
}

export type LoginRequest = {
  email: string
  password: string
}

export type AuthResponse = {
  accessToken: string
  refreshToken: string
}

export type CurrentUserResponse = {
  id: string
  email: string
}

export const authApi = {
  register: (request: RegisterRequest) =>
    httpClient.post<AuthResponse>('/api/auth/register', request),

  login: (request: LoginRequest) =>
    httpClient.post<AuthResponse>('/api/auth/login', request),

  me: () => httpClient.get<CurrentUserResponse>('/api/auth/me'),
}
