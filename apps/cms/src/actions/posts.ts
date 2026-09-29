import { addToast } from "@heroui/react"
import {
    deletePost,
    hidePost,
    publishPost,
    createPost,
} from "@repo/client-api/posts"
import { ActionFunctionArgs, data, redirect } from "react-router"
import { parseErrorResponse } from "../utils/parseErrorResponse"

export const DELETE_INTENT = "delete"
export const PUBLISH_INTENT = "publish"
export const HIDE_INTENT = "hide"

export async function postsAction({ request, params }: ActionFunctionArgs) {
    const { method } = request
    const { postSlug } = params

    const formData = await request.formData()
    if (method === "POST" && !postSlug) {
        return await createNewPost(formData)
    }

    if (postSlug) {
        const intent = formData.get("intent")

        switch (intent) {
            case DELETE_INTENT:
                return await deletePostAction(postSlug)
            case PUBLISH_INTENT:
                return await publishPostAction(postSlug)
            case HIDE_INTENT:
                return await hidePostAction(postSlug)
            default:
                throw data({ message: "Invalid intent" }, 400)
        }
    }

    throw data({ message: "Invalid action" }, 400)
}

async function createNewPost(formData: FormData) {
    try {
        const title = formData.get("title")
        if (!title) {
            throw new Error("Create Tag Action title param missing")
        }

        const newPost = await createPost({ title: title.toString() })

        addToast({
            title: "Success",
            description: "Your new article was successfully created.",
            color: "success",
        })
        const { slug } = newPost
        return redirect(`/posts/${slug}`)
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to create a new article",
                description: `[${status}] - ${errorMessage}`,
                color: "danger",
            })
            return errorResponse
        }
        return error
    }
}

async function deletePostAction(postSlug: string) {
    try {
        await deletePost(postSlug)
        addToast({
            title: "Success",
            description: "Post deleted successfully",
            color: "success",
        })
        return redirect("/")
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to delete post",
                description: `${status || 500} : ${errorMessage}`,
                color: "danger",
            })
            return errorResponse
        }
        return error
    }
}

async function publishPostAction(postSlug: string) {
    try {
        await publishPost(postSlug)
        addToast({
            title: "Success",
            description: "Post published successfully",
            color: "success",
        })
        return null
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to publish post",
                description: `${status || 500} : ${errorMessage}`,
                color: "danger",
            })
            return errorResponse
        }
        return error
    }
}

async function hidePostAction(postSlug: string) {
    try {
        await hidePost(postSlug)
        addToast({
            title: "Success",
            description: "Post hidden successfully",
            color: "success",
        })
        return null
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to hide post",
                description: `${status || 500} : ${errorMessage}`,
                color: "danger",
            })
            return errorResponse
        }
        return error
    }
}
