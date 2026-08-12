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

export const deleteTag = async (
    slug: string,
    accessToken: string
): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags/${slug}`, apiUrl)

    const response = await fetch(url, {
        mode: "cors",
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
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
    slug: string,
    accessToken: string
): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags/${slug}`, apiUrl)

    const response = await fetch(url, {
        mode: "cors",
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
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

export const createTag = async (
    { name, slug }: CreateTagParams,
    accessToken: string
): Promise<TagDetails> => {
    checkApiUrlEnvVariable()

    const apiUrl = import.meta.env.VITE_API_URL
    const url = new URL(`./tags`, apiUrl)

    const response = await fetch(url, {
        mode: "cors",
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name, slug }),
    })
    if (!response.ok) {
        throw response
    }

    const createdTag = await response.json()
    return createdTag
}
