import { Button, Link } from "@heroui/react"
import EditIcon from "@repo/ui/components/Icons/EditIcon"
import { FetcherWithComponents } from "react-router"

export interface EditPostButtonProps {
    postSlug: string
    fetcher: FetcherWithComponents<unknown>
}

export default function EditPostButton({
    postSlug,
    fetcher,
}: EditPostButtonProps) {
    const busy = fetcher.state !== "idle"

    return (
        <Button
            href={`/posts/${postSlug}/edit`}
            as={Link}
            startContent={<EditIcon />}
            color="primary"
            className="w-full font-medium"
            isDisabled={busy}
        >
            Edit
        </Button>
    )
}
