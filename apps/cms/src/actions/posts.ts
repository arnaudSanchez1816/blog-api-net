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

export async function postsAction(
    { request, params }: ActionFunctionArgs,
    accessToken: string
) {
    const { method } = request
    const { postSlug } = params

    const formData = await request.formData()
    if (method === "POST" && !postSlug) {
        return await createNewPost(formData, accessToken)
    }

    if (postSlug) {
        const intent = formData.get("intent")

        switch (intent) {
            case DELETE_INTENT:
                return await deletePostAction(postSlug, accessToken)
            case PUBLISH_INTENT:
                return await publishPostAction(postSlug, accessToken)
            case HIDE_INTENT:
                return await hidePostAction(postSlug, accessToken)
            default:
                throw data({ message: "Invalid intent" }, 400)
        }
    }

    throw data({ message: "Invalid action" }, 400)
}

async function createNewPost(formData: FormData, accessToken: string) {
    try {
        const title = formData.get("title")
        if (!title) {
            throw new Error("Create Tag Action title param missing")
        }

        const newPost = await createPost(
            { title: title.toString() },
            accessToken
        )

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

async function deletePostAction(postSlug: string, accessToken: string) {
    try {
        await deletePost(postSlug, accessToken)
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

async function publishPostAction(postSlug: string, accessToken: string) {
    try {
        await publishPost(postSlug, accessToken)
        addToast({
            title: "Success",
            description: "Post published successfully",
            color: "success",
        })
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

async function hidePostAction(postSlug: string, accessToken: string) {
    try {
        await hidePost(postSlug, accessToken)
        addToast({
            title: "Success",
            description: "Post hidden successfully",
            color: "success",
        })
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
