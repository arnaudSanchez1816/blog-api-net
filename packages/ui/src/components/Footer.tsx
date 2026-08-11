import { Divider, Link } from "@heroui/react"
import GithubIcon from "./Icons/GithubIcon"
import { ReactElement } from "react"

export interface FooterProps {
    children?: ReactElement
}

export default function Footer({ children }: FooterProps) {
    return (
        <footer className="px-6 pb-6 md:pb-12">
            <div className="m-auto max-w-prose">
                <Divider
                    orientation="horizontal"
                    className="mb-8 mt-16 xl:mt-36"
                />
                <div className="[&>*]:not-first:mt-10">
                    <div>
                        <Link href="#" color="primary">
                            ↑ Back to top
                        </Link>
                    </div>
                    {children}
                </div>
                <Divider orientation="horizontal" className="my-8" />
                <div className="text-foreground/70 flex items-center justify-center gap-1 text-sm md:justify-between">
                    <p>© 2025</p>
                    <span className="md:hidden">-</span>
                    <Link
                        className="text-sm md:flex md:gap-1"
                        href="https://github.com/arnaudSanchez1816/blog-api-net"
                        color="foreground"
                        underline="hover"
                    >
                        <GithubIcon className="hidden md:inline-block" />
                        Source
                    </Link>
                </div>
            </div>
        </footer>
    )
}
