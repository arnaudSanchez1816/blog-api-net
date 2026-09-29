import { fetchAccessToken } from "./auth"

// This module is used to do an authenticated fetch.
// This will automatically get the current access token and attempt a retry
// when the initial fetch receive a 401 response.
// A new access token will be fetch and if successful, the initial fetch
// is then retried.

// This var represent the current accessToken refresh try
// No need to send multiple refresh if one is already ongoing
let inFlight: Promise<string> | null = null

const refreshAccessToken = async (): Promise<string> => {
    try {
        inFlight ??= fetchAccessToken()
        const token = await inFlight
        config?.onRefreshed(token)
        return token
    } catch (error) {
        if (error instanceof Response) {
            config?.onRefreshFailed(error)
        }
        throw error
    } finally {
        inFlight = null
    }
}

interface AuthFetchConfig {
    getToken: () => string | null
    onRefreshed: (token: string) => void
    onRefreshFailed: (response: Response) => void
}

let config: AuthFetchConfig = {
    getToken: () => null,
    onRefreshed: () => {},
    onRefreshFailed: () => {},
}

export const configureAuthFetch = (c: AuthFetchConfig) => {
    config = c
}

const withAuth = (init: RequestInit, token: string | null): RequestInit => ({
    ...init,
    mode: "cors",
    headers: {
        ...init.headers,
        ...(token && { Authorization: `Bearer ${token}` }),
    },
})

export const authFetch = async (
    url: URL | string,
    init: RequestInit = {}
): Promise<Response> => {
    const response = await fetch(url, withAuth(init, config.getToken()))

    if (response.status !== 401) {
        return response
    }

    let token: string
    try {
        token = await refreshAccessToken()
    } catch {
        // Refresh failed, return original response
        return response
    }
    // Retry the initial fetch
    return await fetch(url, withAuth(init, token))
}
