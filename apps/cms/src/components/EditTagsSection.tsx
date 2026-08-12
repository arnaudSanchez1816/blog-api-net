import { Autocomplete, AutocompleteItem, Chip } from "@heroui/react"
import { TagDetails } from "@repo/client-api/tags"
import _ from "lodash"
import { Key, useRef } from "react"

interface TagProps {
    tag: TagDetails
    onDeleteTag: (tag: TagDetails) => void
}

function Tag({ tag, onDeleteTag }: TagProps) {
    const { name } = tag

    return (
        <Chip color="secondary" onClose={() => onDeleteTag(tag)} size="md">
            {name}
        </Chip>
    )
}

export interface EditTagsSectionProps {
    allTags: TagDetails[]
    newTags: TagDetails[]
    setNewTags: (newTags: TagDetails[]) => void
}

export default function EditTagsSection({
    allTags,
    newTags,
    setNewTags,
}: EditTagsSectionProps) {
    const tagInputRef = useRef<HTMLInputElement | null>(null)

    const availableTags = _.differenceWith(
        allTags,
        newTags,
        (tag1, tag2) => tag1.id === tag2.id
    )

    const onTagSelected = (tagId: Key | null) => {
        if (tagId) {
            const newTag = allTags.find((t) => t.id === tagId)
            if (!newTag) {
                console.error("Tag does not exists")
                return
            }
            setNewTags([...newTags, newTag])
            tagInputRef.current?.blur()
        }
    }

    const onTagDeleted = ({ id }: TagDetails) => {
        setNewTags(newTags.filter((t) => t.id !== id))
    }

    return (
        <div className="flex flex-wrap items-center gap-2">
            {newTags.map((tag) => (
                <Tag key={tag.id} tag={tag} onDeleteTag={onTagDeleted} />
            ))}
            <Autocomplete
                ref={tagInputRef}
                onSelectionChange={onTagSelected}
                shouldCloseOnBlur
                label="Add tag"
                size="sm"
                className="max-w-3xs"
                radius="full"
            >
                {availableTags.map((tag) => (
                    <AutocompleteItem key={tag.id}>{tag.name}</AutocompleteItem>
                ))}
            </Autocomplete>
        </div>
    )
}
