import { Divider, Link } from "@heroui/react"
import { format } from "date-fns"
import { SvgIconProps } from "../types/icons"
import { PostDetails } from "@repo/client-api/posts"
import { ComponentProps } from "react"

function CommentIcon({
    size = 24,
    strokeWidth = 2,
    width,
    height,
    ...props
}: SvgIconProps) {
    return (
        <svg
            width={width || size}
            height={height || size}
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            strokeWidth={strokeWidth}
            {...props}
        >
            <path
                d="M7.5 10.5H7.51M12 10.5H12.01M16.5 10.5H16.51M9.9 19.2L11.36 21.1467C11.5771 21.4362 11.6857 21.5809 11.8188 21.6327C11.9353 21.678 12.0647 21.678 12.1812 21.6327C12.3143 21.5809 12.4229 21.4362 12.64 21.1467L14.1 19.2C14.3931 18.8091 14.5397 18.6137 14.7185 18.4645C14.9569 18.2656 15.2383 18.1248 15.5405 18.0535C15.7671 18 16.0114 18 16.5 18C17.8978 18 18.5967 18 19.1481 17.7716C19.8831 17.4672 20.4672 16.8831 20.7716 16.1481C21 15.5967 21 14.8978 21 13.5V7.8C21 6.11984 21 5.27976 20.673 4.63803C20.3854 4.07354 19.9265 3.6146 19.362 3.32698C18.7202 3 17.8802 3 16.2 3H7.8C6.11984 3 5.27976 3 4.63803 3.32698C4.07354 3.6146 3.6146 4.07354 3.32698 4.63803C3 5.27976 3 6.11984 3 7.8V13.5C3 14.8978 3 15.5967 3.22836 16.1481C3.53284 16.8831 4.11687 17.4672 4.85195 17.7716C5.40326 18 6.10218 18 7.5 18C7.98858 18 8.23287 18 8.45951 18.0535C8.76169 18.1248 9.04312 18.2656 9.2815 18.4645C9.46028 18.6137 9.60685 18.8091 9.9 19.2ZM8 10.5C8 10.7761 7.77614 11 7.5 11C7.22386 11 7 10.7761 7 10.5C7 10.2239 7.22386 10 7.5 10C7.77614 10 8 10.2239 8 10.5ZM12.5 10.5C12.5 10.7761 12.2761 11 12 11C11.7239 11 11.5 10.7761 11.5 10.5C11.5 10.2239 11.7239 10 12 10C12.2761 10 12.5 10.2239 12.5 10.5ZM17 10.5C17 10.7761 16.7761 11 16.5 11C16.2239 11 16 10.7761 16 10.5C16 10.2239 16.2239 10 16.5 10C16.7761 10 17 10.2239 17 10.5Z"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
            />
        </svg>
    )
}

function ArrowRightIcon({
    size = 24,
    strokeWidth = 1.5,
    width,
    height,
    ...props
}: SvgIconProps) {
    return (
        <svg
            width={width || size}
            height={height || size}
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            strokeWidth={strokeWidth}
            {...props}
        >
            <path
                d="M4 12H20M20 12L14 6M20 12L14 18"
                stroke="currentColor"
                strokeLinecap="round"
                strokeLinejoin="round"
            />
        </svg>
    )
}

export interface PostItemProps {
    post: Omit<PostDetails, "body">
    className: ComponentProps<"article">["className"]
}

export default function PostItem({ post, className = "" }: PostItemProps) {
    const {
        slug,
        title,
        description,
        readingTime,
        publishedAt,
        commentsCount,
    } = post
    const postHref = `/posts/${slug}`

    return (
        <article className={className}>
            <header>
                <Link color="foreground" underline="hover" href={postHref}>
                    <h1 className="text-2xl font-medium hover:cursor-pointer">
                        {title}
                    </h1>
                </Link>
                <div className="text-foreground/70 mt-2 flex gap-2">
                    {publishedAt ? (
                        <Link
                            color="primary"
                            underline="always"
                            href={postHref}
                        >
                            <time dateTime={format(publishedAt, "yyyy-MM-dd")}>
                                {format(publishedAt, "MMMM dd, y")}
                            </time>
                        </Link>
                    ) : (
                        <p className="text-warning">Unpublished</p>
                    )}
                    <span>•</span>
                    <span>{`${readingTime} min read`}</span>
                </div>
            </header>
            <div className="my-4">{description}</div>
            <Divider orientation="horizontal" className="my-4" />
            <footer>
                <div className="flex justify-between">
                    <div>
                        <Link
                            href={`${postHref}#comments`}
                            className="flex gap-1"
                            color="foreground"
                        >
                            <span
                                className="font-bold"
                                aria-description="Number of comments"
                            >
                                {commentsCount}
                            </span>
                            <CommentIcon />
                        </Link>
                    </div>
                    <Link href={postHref} className="font-medium">
                        Read more
                        <ArrowRightIcon />
                    </Link>
                </div>
            </footer>
        </article>
    )
}
