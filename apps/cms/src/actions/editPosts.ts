import { ActionFunctionArgs, data, redirect } from "react-router"
import { parseErrorResponse } from "../utils/parseErrorResponse"
import { addToast } from "@heroui/react"
import { updatePost } from "@repo/client-api/posts"

export async function editPostsActions(
    { request, params }: ActionFunctionArgs,
    accessToken: string
) {
    const { method } = request
    const { postSlug } = params

    if (!postSlug) {
        throw data({ message: "Invalid slug" }, 400)
    }

    if (method.toUpperCase() === "PUT") {
        return await updatePostAction(postSlug, request, accessToken)
    }

    throw data({ message: "Invalid method" }, 400)
}

async function updatePostAction(
    slug: string,
    request: Request,
    accessToken: string
) {
    try {
        const updatedPostData = await request.json()
        const updatedPost = await updatePost(slug, updatedPostData, accessToken)

        addToast({
            title: "Success",
            description: "Post updated successfully",
            color: "success",
        })

        return redirect(`/posts/${updatedPost.slug}`)
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to update post",
                description: `${status} : ${errorMessage}`,
                color: "danger",
            })

            return errorResponse
        }

        throw error
    }
}
