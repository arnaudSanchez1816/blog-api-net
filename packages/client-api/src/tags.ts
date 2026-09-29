import { authFetch } from "./authFetch"
import { checkApiUrlEnvVariable } from "./utils"

export interface TagDetails {
    id: string
    name: string
    slug: string
}

export interface FetchTagsResult {
    metadata: {
        count: number
    }
    results: TagDetails[]
}

export const fetchTags = async (): Promise<FetchTagsResult> => {
    checkApiUrlEnvVariable()
    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags`, apiUrl)

    const response = await fetch(url, { mode: "cors" })
    if (!response.ok) {
        throw response
    }

    const tags = await response.json()

    return tags
}

export const deleteTag = async (slug: string): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags/${slug}`, apiUrl)

    const response = await authFetch(url, {
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
        },
    })
    if (!response.ok) {
        throw response
    }

    const body = await response.json()
    return body
}

interface EditTagParams {
    name: string
    newSlug: string
}

export const editTag = async (
    { name, newSlug }: EditTagParams,
    slug: string
): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags/${slug}`, apiUrl)

    const response = await authFetch(url, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ name, slug: newSlug }),
    })
    if (!response.ok) {
        throw response
    }

    const updatedTag = await response.json()
    return updatedTag
}

type CreateTagParams = Pick<TagDetails, "name" | "slug">

export const createTag = async ({
    name,
    slug,
}: CreateTagParams): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags`, apiUrl)

    const response = await authFetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ name, slug }),
    })
    if (!response.ok) {
        throw response
    }

    const createdTag = await response.json()
    return createdTag
}
