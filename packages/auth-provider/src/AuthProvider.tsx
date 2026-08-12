import {
    createContext,
    useCallback,
    useLayoutEffect,
    useMemo,
    useState,
} from "react"
import { fetchAccessToken } from "@repo/client-api/auth"
import { fetchCurrentUser, UserDetails } from "@repo/client-api/users"
import type { ReactNode } from "react"

export interface AuthContextProps {
    user: UserDetails | null
    accessToken: string | null
    login: ({ email, password }: LoginParams) => Promise<{
        user?: UserDetails
        error?: string
    }>
    logout: () => void
}

export const AuthContext = createContext<AuthContextProps | null>(null)

export interface LoginParams {
    email: string
    password: string
}

export const AuthProvider = ({
    children,
    loaderComponent,
}: {
    children: ReactNode
    loaderComponent: React.ReactElement
}) => {
    const [user, setUser] = useState<UserDetails | null>(null)
    const [accessToken, setAccessToken] = useState<string | null>(null)
    const [init, setInit] = useState(false)

    useLayoutEffect(() => {
        const controller = new AbortController()

        const initAuthProvider = async () => {
            try {
                const token = await fetchAccessToken(controller.signal)
                setAccessToken(token)
                const user = await fetchCurrentUser(token, controller.signal)
                setUser(user)
            } catch (error) {
                if (controller.signal.aborted) {
                    return
                }
                if (!(error instanceof Response)) {
                    console.error(error)
                }
                setAccessToken(null)
                setUser(null)
            } finally {
                if (!controller.signal.aborted) {
                    setInit(true)
                }
            }
        }
        initAuthProvider()

        return () => {
            controller.abort()
        }
    }, [])

    const login = useCallback(
        async ({
            email,
            password,
        }: LoginParams): Promise<
            | {
                  user: UserDetails
              }
            | {
                  error: string
              }
        > => {
            try {
                const url = new URL(
                    "./auth/login",
                    import.meta.env.VITE_API_URL
                )
                const response = await fetch(url, {
                    body: JSON.stringify({
                        email,
                        password,
                    }),
                    headers: {
                        "Content-Type": "application/json",
                    },
                    mode: "cors",
                    method: "post",
                    credentials: "include",
                })

                if (!response.ok) {
                    throw response
                }

                const responseJson = await response.json()
                const { user, accessToken } = responseJson

                if (!user) {
                    throw new Error("Expected user is null")
                }

                setUser(user)
                setAccessToken(accessToken)
                return { user }
            } catch (error) {
                console.error(error)
                if (error instanceof Response) {
                    const body = error.body ? await error.json() : {}
                    const { errorMessage } = body.title || {
                        errorMessage: "Failed to login",
                    }

                    return { error: errorMessage }
                } else if (error instanceof Error) {
                    return { error: error.message }
                }
                throw error
            }
        },
        []
    )

    const logout = useCallback(async (): Promise<void | {
        error: string
    }> => {
        try {
            const url = new URL("./auth/logout", import.meta.env.VITE_API_URL)
            const response = await fetch(url, {
                mode: "cors",
                method: "get",
                credentials: "include",
            })

            if (!response.ok) {
                throw response
            }
        } catch (error) {
            if (error instanceof Response) {
                console.error(error)
                const body = error.body ? await error.json() : {}
                const { errorMessage } = body.title || {
                    errorMessage: "Failed to logout",
                }

                return { error: errorMessage }
            }
        } finally {
            setUser(null)
            setAccessToken(null)
        }
    }, [])

    const providerValue = useMemo(() => {
        return { user, accessToken, login, logout }
    }, [user, accessToken, login, logout])

    if (init === false) {
        return loaderComponent || <div>Loading</div>
    }

    return (
        <AuthContext.Provider value={providerValue}>
            {children}
        </AuthContext.Provider>
    )
}
