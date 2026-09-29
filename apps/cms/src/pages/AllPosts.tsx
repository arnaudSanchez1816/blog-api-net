import { useSearchParams } from "react-router"
import useQuery from "@repo/ui/hooks/useQuery"
import PostsListSkeleton from "@repo/ui/components/PostsList/PostsListSkeleton"
import PostsList from "@repo/ui/components/PostsList/PostsList"
import { useCallback, useEffect, useState } from "react"
import { fetchPosts } from "@repo/client-api/posts"
import { Button } from "@heroui/react"
import PencilIcon from "@repo/ui/components/Icons/PencilIcon"
import NewArticleModal from "../components/modals/NewArticleModal"
import SearchParamsSelect from "@repo/ui/components/Search/SearchParamsSelect"
import SearchParamsToggle from "@repo/ui/components/Search/SearchParamsToggle"
import useParamSearchParams from "@repo/ui/hooks/useParamSearchParams"
import { useSearchLayoutContext } from "../../../../packages/ui/src/components/layouts/SearchLayout"

const DEFAULT_PAGE_SIZE = 10

interface AllPostsQueryParams {
    searchParams: URLSearchParams
}

async function allPostsQuery({ searchParams }: AllPostsQueryParams) {
    const page = Number(searchParams.get("page"))
    const pageSize = Number(searchParams.get("pageSize")) || DEFAULT_PAGE_SIZE
    const sortBy = searchParams.get("sortBy")
    const showUnpublished = searchParams.get("unpublished") === "true"

    return fetchPosts({
        page,
        pageSize,
        sortBy,
        showUnpublished,
    })
}

function NewPostButton() {
    const [modalOpen, setModalOpen] = useState(false)

    return (
        <>
            <Button
                color="primary"
                className="font-medium"
                size="lg"
                startContent={<PencilIcon />}
                onPress={() => setModalOpen(true)}
            >
                New post
            </Button>

            <NewArticleModal
                isOpen={modalOpen}
                onClose={() => setModalOpen(false)}
                onOpenChange={(value) => setModalOpen(value)}
            />
        </>
    )
}

export default function AllPosts() {
    const [searchParams] = useSearchParams()
    const queryFn = useCallback(() => {
        return allPostsQuery({ searchParams })
    }, [searchParams])
    const [allPostsData, isLoading, errors] = useQuery({
        queryKey: ["posts"],
        queryFn,
    })
    const [currentPageString, setCurrentPage] = useParamSearchParams("page", 0)
    const currentPage = Number(currentPageString)

    const [, setLeftContent] = useSearchLayoutContext()
    useEffect(() => {
        setLeftContent(
            <>
                <div className="min-w-38 mt-4">
                    <p className="text-lg font-medium">Filters</p>
                    <div className="flex flex-col gap-2">
                        <SearchParamsSelect
                            paramName="sortBy"
                            items={[
                                { key: "-publishedAt", label: "↓ published" },
                                { key: "publishedAt", label: "↑ published" },
                                { key: "-id", label: "↓ creation" },
                                { key: "id", label: "↑ creation" },
                            ]}
                            defaultValue="-publishedAt"
                        />
                        <SearchParamsToggle
                            onValue={"true"}
                            offValue={"false"}
                            defaultState={false}
                            paramName={"unpublished"}
                            text={"Show unpublished"}
                        />
                    </div>
                    <div className="mt-8 flex justify-start xl:justify-center">
                        <NewPostButton />
                    </div>
                </div>
            </>
        )
        return () => setLeftContent(undefined)
    }, [setLeftContent])

    if (isLoading) {
        return <PostsListSkeleton nbPosts={10} />
    }

    if (errors) {
        return <div>Something went wrong !</div>
    }

    if (!allPostsData) {
        return <div>No posts</div>
    }

    const { results, metadata } = allPostsData

    return (
        <PostsList
            posts={results}
            pagination={{
                currentPage,
                setCurrentPage,
                count: metadata.count,
                pageSize: metadata.pageSize || DEFAULT_PAGE_SIZE,
            }}
        />
    )
}
