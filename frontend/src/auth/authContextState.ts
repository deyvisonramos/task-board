import { createContext } from 'react'
import type { CurrentUserResponse } from '../api/authApi'

export type AuthContextValue = {
  user: CurrentUserResponse | null
  isAuthenticated: boolean
  isInitializing: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
