import EyeIcon from "@repo/ui/components/Icons/EyeIcon"
import { Button } from "@heroui/react"
import { HIDE_INTENT } from "../../actions/posts"
import { FetcherWithComponents } from "react-router"

export interface HidePostButtonProps {
    postSlug: string
    fetcher: FetcherWithComponents<unknown>
}

export default function HidePostButton({
    postSlug,
    fetcher,
}: HidePostButtonProps) {
    const busy = fetcher.state !== "idle"
    const intent = fetcher.formData?.get("intent") || null
    const isBusyButton = intent === HIDE_INTENT
    return (
        <fetcher.Form method="PUT" action={`/posts/${postSlug}`}>
            <Button
                color="warning"
                startContent={<EyeIcon eyeOpen={false} />}
                className="w-full font-medium"
                isLoading={busy && isBusyButton}
                isDisabled={busy && !isBusyButton}
                type="submit"
                name="intent"
                spinnerPlacement="end"
                value={HIDE_INTENT}
            >
                Hide
            </Button>
        </fetcher.Form>
    )
}
