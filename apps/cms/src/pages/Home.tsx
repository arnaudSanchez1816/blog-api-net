import NavLink from "@repo/ui/components/NavLink"
import useQuery from "@repo/ui/hooks/useQuery"
import PostsList from "@repo/ui/components/PostsList/PostsList"
import { useCallback } from "react"
import { fetchUserPosts } from "@repo/client-api/users"
import PostsListSkeleton from "@repo/ui/components/PostsList/PostsListSkeleton"
import { Alert } from "@heroui/react"
import useAuth from "@repo/auth-provider/useAuth"

const NB_RECENT_POSTS = 3

function RecentPosts() {
    const fetchRecentPostsQuery = useCallback(
        () => fetchUserPosts({ pageSize: NB_RECENT_POSTS, sortBy: "-id" }),
        []
    )
    const [data, loading, errors] = useQuery({
        queryFn: fetchRecentPostsQuery,
        queryKey: ["recent", "posts"],
        enabled: true,
    })

    if (loading) {
        return <PostsListSkeleton nbPosts={NB_RECENT_POSTS} />
    }

    if (errors) {
        return <Alert color="danger" title="Failed to fetch recent posts." />
    }

    const { results: recentPosts } = data!

    return <PostsList posts={recentPosts} />
}

export default function Home() {
    const { user } = useAuth()
    return (
        <div>
            <div className="text-2xl">
                <h1>
                    Welcome
                    <div className="mt-2 font-bold">{user!.name}</div>
                </h1>
            </div>
            <div className="mt-4 flex flex-col gap-2">
                <NavLink href="/posts">View all posts</NavLink>
                <NavLink href="/tags">Manage tags</NavLink>
            </div>
            <div className="mt-8">
                <h2 className="text-2xl font-medium">My recent articles</h2>
                <div className="mt-4">
                    <RecentPosts />
                </div>
            </div>
        </div>
    )
}
