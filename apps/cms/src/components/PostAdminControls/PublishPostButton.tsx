import EyeIcon from "@repo/ui/components/Icons/EyeIcon"
import { Button } from "@heroui/react"
import { PUBLISH_INTENT } from "../../actions/posts"
import { FetcherWithComponents } from "react-router"

export interface PublishPostButtonProps {
    postSlug: string
    fetcher: FetcherWithComponents<unknown>
}

export default function PublishPostButton({
    postSlug,
    fetcher,
}: PublishPostButtonProps) {
    const busy = fetcher.state !== "idle"
    const intent = fetcher.formData?.get("intent") || null
    const isBusyButton = intent === PUBLISH_INTENT
    return (
        <fetcher.Form method="PUT" action={`/posts/${postSlug}`}>
            <Button
                color="secondary"
                startContent={<EyeIcon eyeOpen />}
                className="w-full font-medium"
                isLoading={busy && isBusyButton}
                isDisabled={busy && !isBusyButton}
                type="submit"
                name="intent"
                spinnerPlacement="end"
                value={PUBLISH_INTENT}
            >
                Publish
            </Button>
        </fetcher.Form>
    )
}
