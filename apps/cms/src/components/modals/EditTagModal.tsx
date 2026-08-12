import {
    Button,
    Form,
    Input,
    Modal,
    ModalBody,
    ModalContent,
    ModalFooter,
    ModalHeader,
} from "@heroui/react"
import { FormEvent, useId, useRef, useState } from "react"
import { z } from "zod"
import { tagSchema } from "@repo/zod-schemas"
import { useFetcher } from "react-router"
import { TagDetails } from "@repo/client-api/tags"

export interface EditTagModalProps {
    tag: TagDetails
    isOpen: boolean
    setOpen: (isOpen: boolean) => void
}

export default function EditTagModal({
    tag,
    isOpen,
    setOpen,
}: EditTagModalProps) {
    const formId = useId()
    const [errors, setErrors] = useState<Record<string, string | string[]>>({})
    const formRef = useRef(null)
    const fetcher = useFetcher()
    const isLoading = fetcher.state !== "idle"
    const [name, setName] = useState(tag.name)
    const [slug, setSlug] = useState(tag.slug)
    const { slug: originalSlug } = tag

    const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault()
        const form = e.currentTarget

        const formDataObj = Object.fromEntries(new FormData(form))

        try {
            setErrors({})
            // Check form validation
            const editTagValidator = tagSchema.pick({
                name: true,
                slug: true,
            })
            await editTagValidator.parseAsync(formDataObj)
            // Submit form
            await fetcher.submit(form)
            // Handle possible errors or close modal if success
            const { data } = fetcher
            if (data?.error) {
                throw data.error
            }

            setOpen(false)
        } catch (error) {
            if (error instanceof z.ZodError) {
                setErrors(z.flattenError(error).fieldErrors)
            }

            const { details } = error as {
                details?: Record<string, string | string[]>
            }
            if (details) {
                setErrors(details)
            }
        }
    }

    const onOpenChange = (isOpen: boolean) => {
        if (isLoading) {
            return
        }

        if (!isOpen) {
            setName(tag.name)
            setSlug(tag.slug)
            setErrors({})
        }
        setOpen(isOpen)
    }

    return (
        <Modal
            isOpen={isOpen}
            onOpenChange={onOpenChange}
            hideCloseButton={isLoading}
            isDismissable={!isLoading}
        >
            <ModalContent>
                {() => (
                    <>
                        <ModalHeader className="flex flex-col gap-1">
                            Edit tag <span>{tag.name}</span>
                        </ModalHeader>
                        <ModalBody>
                            <Form
                                ref={formRef}
                                id={formId}
                                onSubmit={onSubmit}
                                validationErrors={errors}
                                action="/tags"
                                // @ts-expect-error We check manually the method used in the route action handler
                                method="put"
                            >
                                <Input
                                    type="hidden"
                                    name="originalSlug"
                                    isRequired
                                    value={originalSlug}
                                />
                                <Input
                                    type="text"
                                    name="name"
                                    label="Name"
                                    isRequired
                                    labelPlacement="outside-top"
                                    isDisabled={isLoading}
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                />
                                <Input
                                    type="text"
                                    name="slug"
                                    label="Slug"
                                    isRequired
                                    labelPlacement="outside-top"
                                    isDisabled={isLoading}
                                    value={slug}
                                    onChange={(e) => setSlug(e.target.value)}
                                    errorMessage={({ validationErrors }) => (
                                        <ul>
                                            {validationErrors.map(
                                                (error, i) => (
                                                    <li key={i}>{error}</li>
                                                )
                                            )}
                                        </ul>
                                    )}
                                />
                            </Form>
                        </ModalBody>
                        <ModalFooter>
                            <Button
                                color="danger"
                                variant="light"
                                onPress={() => setOpen(false)}
                                isDisabled={isLoading}
                            >
                                Close
                            </Button>
                            <Button
                                color="primary"
                                type="submit"
                                form={formId}
                                isLoading={isLoading}
                            >
                                Edit
                            </Button>
                        </ModalFooter>
                    </>
                )}
            </ModalContent>
        </Modal>
    )
}
