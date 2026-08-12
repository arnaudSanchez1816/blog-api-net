import { Divider } from "@heroui/react"
import { fetchPost, PostDetails } from "@repo/client-api/posts"
import CommentsSection, {
    commentsSectionId,
} from "@repo/ui/components/CommentsSection/CommentsSection"
import { postSchema } from "@repo/zod-schemas"
import { useEffect } from "react"
import { LoaderFunctionArgs, useLoaderData, useLocation } from "react-router"
import PostAdminControls from "../components/PostAdminControls/PostAdminControls"
import CommentWithControls from "../components/CommentWithControls"
import PostHeader from "@repo/ui/components/posts/PostHeader"
import PostMarkdown from "@repo/ui/components/posts/PostMarkdown"
import { useSearchLayoutContext } from "@repo/ui/components/layouts/SearchLayout"

export async function postLoader(
    { params }: LoaderFunctionArgs,
    accessToken: string
): Promise<PostDetails> {
    const postSlugSchema = postSchema.pick({ slug: true })
    const { slug } = await postSlugSchema.parseAsync({
        slug: params.postSlug,
    })
    const post = await fetchPost(slug, accessToken)

    return post
}

export default function Post() {
    const post = useLoaderData<PostDetails>()

    const { slug, body, commentsCount } = post

    const [, setLeftContent] = useSearchLayoutContext()
    useEffect(() => {
        setLeftContent(<PostAdminControls post={post} />)
        return () => setLeftContent(undefined)
    }, [setLeftContent, post])

    let commentsAutoFetched = false
    const { hash } = useLocation()

    if (hash && hash === `#${commentsSectionId}`) {
        commentsAutoFetched = true
    }

    return (
        <article>
            <PostHeader post={post} />
            <Divider />
            <div className="mt-8">
                <PostMarkdown>{body}</PostMarkdown>
            </div>
            <Divider className="mb-8 mt-16" />
            <div>
                <CommentsSection
                    postSlug={slug}
                    autoFetch={commentsAutoFetched}
                    commentsCount={commentsCount}
                    commentRender={(comment, { refreshComments }) => (
                        <CommentWithControls
                            key={comment.id}
                            comment={comment}
                            refreshComments={refreshComments}
                        />
                    )}
                />
            </div>
        </article>
    )
}
