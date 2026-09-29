import { addToast } from "@heroui/react"
import { deleteComment } from "@repo/client-api/comments"
import { parseErrorResponse } from "../utils/parseErrorResponse"
import { ActionFunctionArgs, data } from "react-router"

export async function commentsAction({ request, params }: ActionFunctionArgs) {
    const { method } = request
    const { id } = params

    if (method.toUpperCase() === "DELETE") {
        if (!id) {
            throw new Error("Delete comment action id invalid")
        }

        return await deleteCommentAction(id)
    }

    throw data({ message: "Invalid method" }, 400)
}

async function deleteCommentAction(id: string) {
    try {
        const deletedComment = await deleteComment(id)
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
