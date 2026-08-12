import { checkApiUrlEnvVariable, timeoutSignal } from "./utils"

export const fetchAccessToken = async (signal?: AbortSignal): Promise<string> => {
    checkApiUrlEnvVariable()
    const getTokenUrl = new URL("./auth/token", import.meta.env.VITE_API_URL)
    const getTokenResponse = await fetch(getTokenUrl, {
        mode: "cors",
        credentials: "include",
        method: "get",
        signal: timeoutSignal(5000, signal),
    })
    if (!getTokenResponse.ok) {
        throw getTokenResponse
    }
    const { accessToken } = await getTokenResponse.json()

    return accessToken
}
