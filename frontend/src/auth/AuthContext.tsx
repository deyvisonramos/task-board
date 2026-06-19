import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi, type AuthResponse, type CurrentUserResponse } from '../api/authApi'
import { AuthContext, type AuthContextValue } from './authContextState'
import { clearTokens, getAccessToken, saveTokens } from './authTokenStorage'

function authResponseToUser(response: AuthResponse) {
  return {
    id: response.user.id,
    email: response.user.email,
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUserResponse | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  useEffect(() => {
    let isMounted = true

    async function loadCurrentUser() {
      if (!getAccessToken()) {
        setIsInitializing(false)
        return
      }

      try {
        const response = await authApi.me()

        if (isMounted) {
          setUser(response.data)
        }
      } catch {
        clearTokens()

        if (isMounted) {
          setUser(null)
        }
      } finally {
        if (isMounted) {
          setIsInitializing(false)
        }
      }
    }

    void loadCurrentUser()

    return () => {
      isMounted = false
    }
  }, [])

  const completeAuthentication = useCallback((response: AuthResponse) => {
    saveTokens({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
    })
    setUser(authResponseToUser(response))
  }, [])

  const login = useCallback(
    async (email: string, password: string) => {
      const response = await authApi.login({ email, password })
      completeAuthentication(response.data)
    },
    [completeAuthentication],
  )

  const register = useCallback(
    async (email: string, password: string) => {
      const response = await authApi.register({ email, password })
      completeAuthentication(response.data)
    },
    [completeAuthentication],
  )

  const logout = useCallback(() => {
    clearTokens()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isInitializing,
      login,
      register,
      logout,
    }),
    [isInitializing, login, logout, register, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
