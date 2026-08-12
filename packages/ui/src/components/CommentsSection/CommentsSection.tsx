import { Alert, Button } from "@heroui/react"
import Comment from "./Comment"
import CommentSkeleton from "./CommentSkeleton"
import CommentReplyForm from "./CommentReplyForm"
import useQuery from "../../hooks/useQuery"
import { ComponentProps, ReactNode, useCallback } from "react"
import { CommentDetails, fetchComments } from "@repo/client-api/comments"
import useAuth from "@repo/auth-provider/useAuth"

export const commentsSectionId = "comments"

export interface CommentsSectionWrapperProps {
    commentsCount: number
    children: ComponentProps<"div">["children"]
    fetchComments?: () => void
    postSlug: string
}

function CommentsSectionWrapper({
    commentsCount,
    children,
    fetchComments,
    postSlug,
}: CommentsSectionWrapperProps) {
    return (
        <div id={commentsSectionId}>
            <h2 className="text-2xl font-medium">
                {commentsCount > 0 && <span>{commentsCount} </span>}
                {commentsCount === 1 ? "Comment" : "Comments"}
            </h2>
            <div className="mt-8 flex flex-col gap-12">{children}</div>
            <CommentReplyForm
                fetchComments={fetchComments}
                postSlug={postSlug}
            />
        </div>
    )
}

export type RenderCommentCallback = (
    comment: CommentDetails,
    options: { refreshComments: () => void }
) => ReactNode

export interface CommentsSectionProps {
    postSlug: string
    commentsCount: number
    commentRender?: RenderCommentCallback
    autoFetch?: boolean
}

export default function CommentsSection({
    postSlug,
    commentsCount,
    commentRender,
    autoFetch = true,
}: CommentsSectionProps) {
    let accessToken: string | null | undefined
    try {
        const authContext = useAuth()
        accessToken = authContext.accessToken
    } catch {
        /* empty */
    }

    const fetchCommentsQuery = useCallback(() => {
        return fetchComments(postSlug, accessToken)
    }, [postSlug, accessToken])

    const [comments, loading, error, triggerFetch] = useQuery({
        queryFn: fetchCommentsQuery,
        enabled: autoFetch,
        queryKey: ["comments", postSlug],
    })

    if (error) {
        return (
            <CommentsSectionWrapper
                commentsCount={commentsCount}
                fetchComments={triggerFetch}
                postSlug={postSlug}
            >
                <Alert
                    color="danger"
                    title="Something wrong happened when trying to fetch comments"
                />
            </CommentsSectionWrapper>
        )
    }

    if (loading) {
        const skeletons = []
        for (let i = 0; i < commentsCount; ++i) {
            skeletons.push(<CommentSkeleton key={i} />)
        }
        return (
            <CommentsSectionWrapper
                commentsCount={commentsCount}
                fetchComments={triggerFetch}
                postSlug={postSlug}
            >
                {skeletons}
            </CommentsSectionWrapper>
        )
    }

    if (!comments) {
        if (commentsCount <= 0) {
            return (
                <CommentsSectionWrapper
                    commentsCount={commentsCount}
                    fetchComments={triggerFetch}
                    postSlug={postSlug}
                >
                    <div className="flex items-center justify-center">
                        <p className="text-foreground/70 text-lg font-medium">
                            No comments yet
                        </p>
                    </div>
                </CommentsSectionWrapper>
            )
        } else {
            return (
                <CommentsSectionWrapper
                    commentsCount={commentsCount}
                    fetchComments={triggerFetch}
                    postSlug={postSlug}
                >
                    <div className="mt-8 flex justify-center">
                        <Button
                            color="default"
                            onPress={() => triggerFetch()}
                            radius="sm"
                        >
                            View Comments
                        </Button>
                    </div>
                </CommentsSectionWrapper>
            )
        }
    }

    const { results } = comments
    return (
        <CommentsSectionWrapper
            commentsCount={commentsCount}
            fetchComments={triggerFetch}
            postSlug={postSlug}
        >
            {results.length > 0 ? (
                results.map((comment) =>
                    commentRender ? (
                        commentRender(comment, {
                            refreshComments: triggerFetch,
                        })
                    ) : (
                        <Comment key={comment.id} comment={comment} />
                    )
                )
            ) : (
                <div className="flex items-center justify-center">
                    <p className="text-foreground/70 text-lg font-medium">
                        No comments yet
                    </p>
                </div>
            )}
        </CommentsSectionWrapper>
    )
}
