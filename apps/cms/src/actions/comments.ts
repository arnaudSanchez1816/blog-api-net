import { addToast } from "@heroui/react"
import { deleteComment } from "@repo/client-api/comments"
import { parseErrorResponse } from "../utils/parseErrorResponse"
import { ActionFunctionArgs } from "react-router"

export async function commentsAction(
    { request, params }: ActionFunctionArgs,
    accessToken: string
) {
    const { method } = request
    const { id } = params

    if (method.toUpperCase() === "DELETE") {
        if (!id) {
            throw new Error("Delete comment action id invalid")
        }

        return await deleteCommentAction(id, accessToken)
    }
}

async function deleteCommentAction(id: string, accessToken: string) {
    try {
        const deletedComment = await deleteComment(id, accessToken)
        addToast({
            title: "Success",
            description: "Comment deleted successfully",
            color: "success",
        })
        return deletedComment
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to delete comment",
                description: `${status} : ${errorMessage}`,
                color: "danger",
            })

            return errorResponse
        }

        return error
    }
}
