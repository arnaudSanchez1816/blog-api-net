import { fetchPost, PostDetails } from "@repo/client-api/posts"
import { postSchema } from "@repo/zod-schemas"
import {
    LoaderFunctionArgs,
    useBlocker,
    useFetcher,
    useLoaderData,
} from "react-router"
import { ReactNode, useCallback, useEffect, useState } from "react"
import { addToast, Button, Divider } from "@heroui/react"
import { fetchTags, TagDetails } from "@repo/client-api/tags"
import _ from "lodash"
import EditablePostTitle from "../components/EditablePostTitle"
import EditTagsSection from "../components/EditTagsSection"
import EditPostContentSection from "../components/EditPostContentSection"
import ThreeColumnLayout from "@repo/ui/components/layouts/ThreeColumnLayout"
import SaveIcon from "@repo/ui/components/Icons/SaveIcon"
import z from "zod"
import Confirm from "../components/modals/Confirm"

interface EditPostLoaderReturnValue {
    post: PostDetails
    allTags: TagDetails[]
}

export async function editPostLoader(
    { params }: LoaderFunctionArgs,
    accessToken: string | null
): Promise<EditPostLoaderReturnValue> {
    const postSlugSchema = postSchema.pick({ slug: true })
    const { slug } = await postSlugSchema.parseAsync({ slug: params.postSlug })

    const [post, allTags] = await Promise.all([
        fetchPost(slug, accessToken),
        fetchTags(),
    ])
    return { post, allTags: allTags.results }
}

interface EditPostLayoutProps {
    children?: ReactNode
    left?: ReactNode
    right?: ReactNode
}

function EditPostLayout({ children, left, right }: EditPostLayoutProps) {
    return <ThreeColumnLayout left={left} right={right} center={children} />
}

export default function EditPost() {
    const { post, allTags } = useLoaderData<EditPostLoaderReturnValue>()
    const { slug, title, body, tags } = post
    const [newTitle, setNewTitle] = useState<string>(title)
    const [newBody, setNewBody] = useState<string>(body)
    const [newTags, setNewTags] = useState<TagDetails[]>(tags)
    const [isDirty, setIsDirty] = useState<boolean>(false)
    const blocker = useBlocker(useCallback(() => isDirty, [isDirty]))
    const fetcher = useFetcher()

    const isSaving =
        fetcher.state !== "idle" && fetcher.formAction === `/posts/${slug}/edit`

    const onSave = async () => {
        if (!isDirty) {
            return
        }

        try {
            const newData = {
                title: newTitle,
                body: newBody,
                tags: newTags.map((tag) => tag.id),
            }

            // Check form validation
            const updatePostValidator = postSchema.pick({
                body: true,
                title: true,
                tags: true,
            })
            await updatePostValidator.parseAsync(newData)
            // Submit form
            await fetcher.submit(newData, {
                action: `/posts/${slug}/edit`,
                method: "PUT",
                encType: "application/json",
            })
        } catch (error) {
            if (error instanceof z.ZodError) {
                console.error(z.flattenError(error))
                addToast({
                    title: "Failed to update post",
                    description: "Post contains invalid content",
                    color: "danger",
                })
            }
        }
    }

    useEffect(() => {
        if (fetcher.data?.ok) {
            if (blocker.state === "blocked") {
                // proceed with the blocked navigation
                blocker.proceed()
            }
        }
    }, [blocker, fetcher.data])

    useEffect(() => {
        if (isDirty) {
            return
        }
        if (newTitle !== title) {
            setIsDirty(true)
        }
        if (newBody !== body) {
            setIsDirty(true)
        }
        if (
            _.xorWith(newTags, tags, (newT, oldT) => newT.id === oldT.id)
                .length !== 0
        ) {
            setIsDirty(true)
        }
    }, [newTitle, newBody, newTags, isDirty, title, body, tags])

    return (
        <>
            <EditPostLayout
                left={
                    <div className="min-w-38">
                        <p className="text-2xl font-medium xl:text-3xl">
                            Edit post
                        </p>
                        <Button
                            isLoading={isSaving}
                            color="primary"
                            size="md"
                            className="min-sm:max-xl:max-w-32 mt-4 w-full font-medium"
                            onPress={onSave}
                            isDisabled={!isDirty}
                            startContent={<SaveIcon />}
                        >
                            Save
                        </Button>
                    </div>
                }
            >
                <EditablePostTitle
                    newTitle={newTitle}
                    setNewTitle={setNewTitle}
                />
                <div className="mt-4">
                    <EditTagsSection
                        allTags={allTags}
                        newTags={newTags}
                        setNewTags={setNewTags}
                    />
                </div>
                <Divider className="mt-4" />
                <EditPostContentSection
                    newBody={newBody}
                    setNewBody={setNewBody}
                />
            </EditPostLayout>
            {blocker.state === "blocked" && (
                <Confirm
                    message={
                        "You have unsaved modifications, are you sure you want to leave the current page ?"
                    }
                    onConfirm={() => blocker.proceed()}
                    onCancel={() => blocker.reset()}
                />
            )}
        </>
    )
}
