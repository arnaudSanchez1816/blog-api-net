import { Button, Popover, PopoverContent, PopoverTrigger } from "@heroui/react"
import EditIcon from "@repo/ui/components/Icons/EditIcon"
import DeletePostButton from "./DeletePostButton"
import PublishPostButton from "./PublishPostButton"
import HidePostButton from "./HidePostButton"
import EditPostButton from "./EditPostButton"
import { FetcherWithComponents, useFetcher } from "react-router"
import { PostDetails } from "@repo/client-api/posts"

interface ControlsButtonsGroupProps {
    post: PostDetails
    fetcher: FetcherWithComponents<unknown>
}

function ControlsButtonsGroup({ post, fetcher }: ControlsButtonsGroupProps) {
    const { slug, publishedAt } = post

    return (
        <>
            <EditPostButton postSlug={slug} fetcher={fetcher} />
            <DeletePostButton postSlug={slug} fetcher={fetcher} />
            {!publishedAt ? (
                <PublishPostButton postSlug={slug} fetcher={fetcher} />
            ) : (
                <HidePostButton postSlug={slug} fetcher={fetcher} />
            )}
        </>
    )
}

export interface PostAdminControlsProps {
    post: PostDetails
}

export default function PostAdminControls({ post }: PostAdminControlsProps) {
    const fetcher = useFetcher()

    const fetcherIsLoading = fetcher.state !== "idle"

    return (
        <>
            <div className="min-w-38 hidden lg:block">
                <div className="flex gap-2 xl:flex-col">
                    <ControlsButtonsGroup post={post} fetcher={fetcher} />
                </div>
            </div>
            <Popover
                backdrop="opaque"
                placement="left"
                shouldCloseOnInteractOutside={() => fetcher.state === "idle"}
            >
                <PopoverTrigger>
                    <Button
                        color="primary"
                        radius="full"
                        isIconOnly
                        isLoading={fetcherIsLoading}
                        className="fixed bottom-4 right-4 z-10 lg:hidden"
                    >
                        <EditIcon />
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="p-4">
                    <p className="text-lg font-bold">Controls</p>
                    <div className="mt-4 flex flex-col gap-2">
                        <ControlsButtonsGroup post={post} fetcher={fetcher} />
                    </div>
                </PopoverContent>
            </Popover>
        </>
    )
}
