import { addToast } from "@heroui/react"
import { createTag, deleteTag, editTag } from "@repo/client-api/tags"
import { ActionFunctionArgs, data } from "react-router"
import { parseErrorResponse } from "../utils/parseErrorResponse"

export async function tagsAction(
    { request }: ActionFunctionArgs,
    accessToken: string
) {
    const { method } = request

    const formData = await request.formData()

    if (method.toUpperCase() === "POST") {
        return await createTagAction(formData, accessToken)
    }

    const tagSlug = formData.get("originalSlug")
    if (!tagSlug) {
        throw data({ message: "Invalid tag slug" }, 400)
    }

    if (method.toUpperCase() === "DELETE") {
        return await deleteTagAction(tagSlug.toString(), accessToken)
    }

    if (method.toUpperCase() === "PUT") {
        return await editTagAction(tagSlug.toString(), formData, accessToken)
    }

    throw data({ message: "Invalid action" }, 400)
}

async function createTagAction(formData: FormData, accessToken: string) {
    try {
        const name = formData.get("name")
        if (!name) {
            throw new Error("Create tag missing name parameter")
        }
        const slug = formData.get("slug")
        if (!slug) {
            throw new Error("Create tag missing slug parameter")
        }
        const createdTag = await createTag(
            { name: name.toString(), slug: slug.toString() },
            accessToken
        )

        addToast({
            title: "Tag created",
            description: `${createdTag.name}`,
            color: "success",
        })
        return createdTag
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to create tag",
                description: `${status} : ${errorMessage}`,
                color: "danger",
            })

            return errorResponse
        }

        return error
    }
}

async function editTagAction(
    tagSlug: string,
    formData: FormData,
    accessToken: string
) {
    try {
        const name = formData.get("name")
        if (!name) {
            throw new Error("Edit Tag Action name param missing")
        }
        const slug = formData.get("slug")
        if (!slug) {
            throw new Error("Edit Tag Action slug param missing")
        }
        const editedTag = await editTag(
            { name: name.toString(), newSlug: slug.toString() },
            tagSlug,
            accessToken
        )
        addToast({
            title: "Tag edited",
            description: `${editedTag.name}`,
            color: "success",
        })

        return editedTag
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to edit tag",
                description: `${status} : ${errorMessage}`,
                color: "danger",
            })

            return errorResponse
        }

        return error
    }
}

async function deleteTagAction(tagSlug: string, accessToken: string) {
    try {
        const deletedTag = await deleteTag(tagSlug, accessToken)
        addToast({
            title: "Tag deleted",
            description: `${deletedTag.name}`,
            color: "success",
        })

        return deletedTag
    } catch (error) {
        if (error instanceof Response) {
            const errorResponse = await parseErrorResponse(error)
            const { status, errorMessage } = errorResponse
            addToast({
                title: "Failed to delete tag",
                description: `${status} : ${errorMessage}`,
                color: "danger",
            })

            return errorResponse
        }

        return error
    }
}
