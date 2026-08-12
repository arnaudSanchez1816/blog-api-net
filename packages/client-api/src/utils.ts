export const checkApiUrlEnvVariable = () => {
    const apiUrl = import.meta.env.VITE_API_URL
    if (apiUrl === null || apiUrl === undefined) {
        throw new Error("API_URL env variable is not set, check your .env file")
    }
}

export const timeoutSignal = (ms = 5000, signal?: AbortSignal): AbortSignal => {
    return signal ? AbortSignal.any([signal, AbortSignal.timeout(ms)]) : AbortSignal.timeout(ms)
}
