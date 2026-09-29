import { authFetch } from "./authFetch"
import { checkApiUrlEnvVariable } from "./utils"

export interface PostDetails {
    id: string
    slug: string
    title: string
    description: string
    body: string
    readingTime: number
    publishedAt: Date | null
    commentsCount: number
    author: {
        name: string
        id: string
    }
    tags: {
        name: string
        id: string
        slug: string
    }[]
}

export type PostDetailsWithoutCommentsAndTags = Omit<
    PostDetails,
    "tags" | "commentsCount"
>

export interface CreatePostParams {
    title: string
}

export const createPost = async ({
    title,
}: CreatePostParams): Promise<PostDetails> => {
    checkApiUrlEnvVariable()
    const url = new URL("./posts", import.meta.env.VITE_API_URL)
    const response = await authFetch(url, {
        method: "post",
        headers: {
            "Content-type": "application/json",
        },
        body: JSON.stringify({ title }),
    })

    if (!response.ok) {
        throw response
    }

    const post = await response.json()

    return post
}

export interface FetchPostsParams {
    q?: string | null
    page?: number | null
    pageSize?: number | null
    sortBy?: "publishedAt" | "-publishedAt" | "id" | "-id" | string | null
    showUnpublished?: boolean | null
    tags?: string | string[] | null
}

export interface FetchPostsResult {
    metadata: {
        count: number
        page: number | undefined
        pageSize: number | undefined
        sortBy: "id" | "publishedAt" | "-publishedAt" | "-id" | undefined
        tags: (string | number)[] | undefined
    }
    results: Omit<PostDetails, "body">[]
}

export const fetchPosts = async ({
    q,
    tags,
    page,
    pageSize,
    sortBy,
    showUnpublished = false,
}: FetchPostsParams): Promise<FetchPostsResult> => {
    const searchParams = new URLSearchParams()
    if (page) {
        searchParams.set("page", page.toString())
    }
    if (pageSize) {
        searchParams.set("pageSize", pageSize.toString())
    }
    if (q) {
        searchParams.set("q", q)
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
        searchParams.set("unpublished", "true")
    }

    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts?${searchParams}`, apiUrl)
    const response = await authFetch(url, {
        headers: {
            "Content-Type": "application/json",
        },
    })

    if (!response.ok) {
        throw response
    }
    const { results, ...dataJson } = await response.json()

    return {
        ...dataJson,
        results,
    }
}

export const fetchPost = async (slug: string): Promise<PostDetails> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts/${slug}`, apiUrl)
    const response = await authFetch(url, {
        headers: {
            "Content-Type": "application/json",
        },
    })

    if (!response.ok) {
        throw response
    }

    const post = await response.json()

    return post
}

export const deletePost = async (
    postSlug: string
): Promise<PostDetailsWithoutCommentsAndTags> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts/${postSlug}`, apiUrl)

    const response = await authFetch(url, {
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
        },
    })

    if (!response.ok) {
        throw response
    }

    const post = await response.json()

    return post
}

export const publishPost = async (postSlug: string): Promise<void> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts/${postSlug}`, apiUrl)

    const response = await authFetch(url, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            isPublished: true,
        }),
    })

    if (!response.ok) {
        throw response
    }
}

export const hidePost = async (postSlug: string): Promise<void> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts/${postSlug}`, apiUrl)

    const response = await authFetch(url, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            isPublished: false,
        }),
    })

    if (!response.ok) {
        throw response
    }
}

interface UpdatePostParams {
    body: string
    title: string
    tags: number[]
}

export const updatePost = async (
    postSlug: string,
    { body, title, tags }: UpdatePostParams
): Promise<Omit<PostDetails, "commentsCount">> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./posts/${postSlug}`, apiUrl)
    const response = await authFetch(url, {
        method: "PUT",
        headers: {
            "Content-type": "application/json",
        },
        body: JSON.stringify({ body, title, tags }),
    })

    if (!response.ok) {
        throw response
    }

    const post = await response.json()

    return post
}
