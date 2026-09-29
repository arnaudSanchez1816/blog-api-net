import { authFetch } from "./authFetch"
import { FetchPostsParams, PostDetails } from "./posts"
import { checkApiUrlEnvVariable, timeoutSignal } from "./utils"

export interface UserDetails {
    id: string
    name: string
    email: string
}

export const fetchCurrentUser = async (
    signal?: AbortSignal
): Promise<UserDetails> => {
    checkApiUrlEnvVariable()

    const url = new URL("./users/me", import.meta.env.VITE_API_URL)
    const response = await authFetch(url, {
        headers: {
            "Content-Type": "application/json",
        },
        method: "get",
        signal: timeoutSignal(5000, signal),
    })
    if (!response.ok) {
        throw response
    }
    const user = await response.json()
    return user
}

export interface FetchUserPostsResult {
    metadata: {
        count: number
    }
    results: PostDetails[]
}

export const fetchUserPosts = async ({
    q,
    tags,
    page,
    pageSize,
    sortBy,
    showUnpublished = false,
}: FetchPostsParams): Promise<FetchUserPostsResult> => {
    checkApiUrlEnvVariable()
    const url = new URL("./users/me/posts", import.meta.env.VITE_API_URL)
    const searchParams = new URLSearchParams()
    if (page) {
        searchParams.set("page", page.toString())
    }
    if (pageSize) {
        searchParams.set("pageSize", pageSize.toString())
    }
    if (sortBy) {
        searchParams.set("sortBy", sortBy)
    }
    if (tags) {
        if (typeof tags === "string") {
            tags = [tags]
        }

        if (!Array.isArray(tags)) {
            throw new Error(
                "Invalid tags parameter type, must be either string or array"
            )
        }

        searchParams.set("tags", tags.join(","))
    }
    if (showUnpublished) {
        searchParams.set("unpublished", "")
    }
    if (q) {
        searchParams.set("q", q)
    }

    const response = await authFetch(`${url}?${searchParams}`, {
        method: "get",
        headers: {
            "Content-Type": "application/json",
        },
    })

    if (!response.ok) {
        throw response
    }

    const data = await response.json()
    return data
}
