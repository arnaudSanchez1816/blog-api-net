import { addToast, Button, Form, Input, Textarea, User } from "@heroui/react"
import useAuth from "@repo/auth-provider/useAuth"
import { postComment } from "@repo/client-api/comments"
import { UserDetails } from "@repo/client-api/users"
import { useState } from "react"

export interface CommentReplyFormProps {
    postSlug: string
    fetchComments?: () => void
}

export default function CommentReplyForm({
    postSlug,
    fetchComments,
}: CommentReplyFormProps) {
    let accessToken: string | null | undefined
    let user: UserDetails | null | undefined
    try {
        const authContext = useAuth()
        accessToken = authContext.accessToken
        user = authContext.user
    } catch {
        /* empty */
    }

    const [submitting, setSubmitting] = useState(false)

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault()

        const form = e.currentTarget
        const formData = new FormData(form)
        const { username, body } = Object.fromEntries(formData)

        setSubmitting(true)
        try {
            await postComment(
                {
                    postSlug,
                    username: username as string,
                    commentBody: body as string,
                },
                accessToken
            )

            if (fetchComments) {
                fetchComments()
            }
            addToast({
                title: "Your reply was successfully submitted",
                variant: "solid",
                color: "success",
            })
            form.reset()
        } catch (error) {
            console.error(error)
            addToast({
                title: "Reply submit failed",
                description:
                    "Something wrong happened when trying to submit your reply.",
                variant: "solid",
                color: "danger",
            })
        } finally {
            setSubmitting(false)
        }
    }

    return (
        <div className="mt-8 flex flex-col gap-4">
            <h3 className="text-xl font-medium">Reply</h3>
            <p className="text-danger text-sm">* indicate a required field</p>
            <Form onSubmit={handleSubmit}>
                {user ? (
                    <>
                        <User
                            avatarProps={{
                                showFallback: true,
                                name: user.name,
                            }}
                            name={user.name}
                            description={<p>{user.email}</p>}
                            classNames={{ name: "font-medium" }}
                        />
                        <Input
                            type="hidden"
                            name="username"
                            value={user.name}
                        />
                    </>
                ) : (
                    <Input
                        type="text"
                        isRequired
                        name="username"
                        label="Username"
                        labelPlacement="outside-top"
                        variant="faded"
                    />
                )}
                <Textarea
                    label="Message"
                    labelPlacement="outside"
                    placeholder="Enter your comment"
                    className="w-full"
                    variant="faded"
                    name="body"
                    isRequired
                />
                <div className="mt-4 flex w-full justify-end">
                    <Button
                        type="submit"
                        color="primary"
                        radius="sm"
                        isLoading={submitting}
                    >
                        Submit
                    </Button>
                </div>
            </Form>
        </div>
    )
}
