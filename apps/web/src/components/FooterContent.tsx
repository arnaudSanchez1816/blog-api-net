import { Button, Link } from "@heroui/react"
import GithubIcon from "@repo/ui/components/Icons/GithubIcon"
import LinkedInIcon from "@repo/ui/components/Icons/LinkedInIcon"

export default function FooterContent() {
    return (
        <>
            <div className="flex flex-col gap-x-4 gap-y-2 lg:flex-row">
                <h2 className="text-large grow-0 basis-1/3 font-medium">
                    About
                </h2>
                <div>
                    <p>
                        This blog was made as a personal project to practice API
                        design and as a capstone project for my web developper
                        training. It includes a RESTful API, a front-end app and
                        a CMS app.
                    </p>
                    <p className="mt-4">
                        This project is built with ASP.NET Core, Postgres,
                        React, Tailwind and HeroUI.
                    </p>
                    <Link
                        href="/about"
                        color="foreground"
                        underline="always"
                        className="mt-6"
                    >
                        Read more…
                    </Link>
                </div>
            </div>
            <div className="flex flex-col gap-x-4 gap-y-2 lg:flex-row">
                <h2 className="text-large grow-0 basis-1/3 font-medium">
                    Socials
                </h2>
                <div className="flex grow gap-2">
                    <Button
                        href="https://linkedin.com/in/arnaud-sanchez-b6ba21277"
                        className="rounded-full"
                        color="default"
                        isIconOnly
                        variant="flat"
                        as={Link}
                    >
                        <LinkedInIcon size={24} />
                    </Button>
                    <Button
                        href="https://github.com/arnaudSanchez1816"
                        className="rounded-full"
                        color="default"
                        isIconOnly
                        variant="flat"
                        as={Link}
                    >
                        <GithubIcon size={24} />
                    </Button>
                </div>
            </div>
        </>
    )
}
