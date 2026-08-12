import { checkApiUrlEnvVariable } from "./utils"

export interface CommentDetails {
    id: string
    username: string
    body: string
    createdAt: Date
    postId: string
}

export interface PostCommentParams {
    postSlug: string
    username: string
    commentBody: string
}

export const postComment = async (
    { postSlug, username, commentBody }: PostCommentParams,
    accessToken?: string | null
): Promise<CommentDetails> => {
    checkApiUrlEnvVariable()
    const API_URL = import.meta.env.VITE_API_URL

    const url = new URL(`./posts/${postSlug}/comments`, API_URL)
    const response = await fetch(url, {
        mode: "cors",
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
        },
        body: JSON.stringify({
            username,
            body: commentBody,
        }),
    })

    if (!response.ok) {
        throw response
    }

    const createdComment = await response.json()
    return createdComment
}

export interface FetchCommentsResult {
    metadata: {
        count: number
    }
    results: CommentDetails[]
}

export const fetchComments = async (
    postSlug: string,
    accessToken?: string | null
): Promise<FetchCommentsResult> => {
    if (!postSlug) {
        throw new Error("PostSlug is invalid")
    }

    checkApiUrlEnvVariable()
    const url = new URL(
        `./posts/${postSlug}/comments`,
        import.meta.env.VITE_API_URL
    )
    const response = await fetch(url, {
        mode: "cors",
        method: "get",
        headers: {
            "Content-Type": "application/json",
            ...(accessToken && { Authorization: `Bearer ${accessToken}` }),
        },
    })
    if (!response.ok) {
        throw response
    }
    const comments = await response.json()
    return comments
}

export const deleteComment = async (
    commentId: string,
    accessToken: string
): Promise<CommentDetails> => {
    if (!commentId) {
        throw new Error("Comment id is invalid")
    }

    checkApiUrlEnvVariable()
    const url = new URL(`./comments/${commentId}`, import.meta.env.VITE_API_URL)
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
    const deletedComment = await response.json()
    return deletedComment
}
