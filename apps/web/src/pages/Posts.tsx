import {
    Await,
    LoaderFunctionArgs,
    useLoaderData,
    useLocation,
} from "react-router"
import { Fragment, Suspense, useEffect } from "react"
import useParamSearchParams from "@repo/ui/hooks/useParamSearchParams"
import PostsList from "@repo/ui/components/PostsList/PostsList"
import PostsListSkeleton from "@repo/ui/components/PostsList/PostsListSkeleton"
import { fetchPosts, FetchPostsResult } from "@repo/client-api/posts"
import SortByPublishedButton from "@repo/ui/components/SortByPublishedButton"
import { useSearchLayoutContext } from "@repo/ui/components/layouts/SearchLayout"

const pageSize = 10

interface PostsLoaderReturnValue {
    getPosts: Promise<FetchPostsResult>
}

export const postsLoader = async ({
    request,
}: LoaderFunctionArgs): Promise<PostsLoaderReturnValue> => {
    const url = new URL(request.url)
    const pageTerm = url.searchParams.get("page")
    const pageSizeTerm = url.searchParams.get("pageSize") || pageSize
    const sortBy = url.searchParams.get("sortBy")
    const getPosts = fetchPosts({
        page: Number(pageTerm),
        pageSize: Number(pageSizeTerm),
        sortBy: sortBy,
    })

    return { getPosts }
}

export default function Posts() {
    const location = useLocation()
    const { getPosts } = useLoaderData()
    const [currentPageString, setCurrentPage] = useParamSearchParams("page", 0)
    const [, setLeftContent] = useSearchLayoutContext()

    const currentPage = Number(currentPageString)

    useEffect(() => {
        setLeftContent(<SortByPublishedButton />)
        return () => setLeftContent(undefined)
    }, [setLeftContent])

    return (
        <Fragment key={location.key}>
            <Suspense fallback={<PostsListSkeleton nbPosts={pageSize} />}>
                <Await resolve={getPosts}>
                    {({ results, metadata }) => (
                        <PostsList
                            posts={results}
                            pagination={{
                                currentPage,
                                setCurrentPage,
                                ...metadata,
                            }}
                        />
                    )}
                </Await>
            </Suspense>
        </Fragment>
    )
}
