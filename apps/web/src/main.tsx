import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import {
    createBrowserRouter,
    createRoutesFromElements,
    data,
    Route,
} from "react-router"
import { RouterProvider } from "react-router/dom"
import "./style.css"
import App from "./App"
import Home, { homeLoader } from "./pages/Home"
import About, { aboutLoader } from "./pages/About"
import Posts, { postsLoader } from "./pages/Posts"
import PostPage, { postPageLoader } from "./pages/Post"
import Error from "@repo/ui/components/Error"
import { Spinner } from "@heroui/react"
import Search, { searchLoader } from "@repo/ui/components/Search/Search"
import SearchLayout, {
    searchLayoutLoader,
} from "@repo/ui/components/layouts/SearchLayout"

const router = createBrowserRouter(
    createRoutesFromElements(
        <Route
            path="/"
            element={<App />}
            hydrateFallbackElement={
                <div className="flex h-screen items-center justify-center">
                    <Spinner />
                </div>
            }
        >
            <Route errorElement={<Error />}>
                <Route element={<SearchLayout />} loader={searchLayoutLoader}>
                    <Route
                        index
                        element={<Home />}
                        loader={homeLoader}
                        handle={{
                            title: "Recent posts",
                        }}
                    />
                    <Route
                        path="/search"
                        element={<Search />}
                        loader={searchLoader}
                        handle={{
                            title: "Search",
                        }}
                    />
                    <Route path="/posts">
                        <Route
                            index
                            element={<Posts />}
                            loader={postsLoader}
                            handle={{
                                title: "All posts",
                            }}
                        />
                        <Route
                            path=":postSlug"
                            element={<PostPage />}
                            loader={postPageLoader}
                        />
                    </Route>
                </Route>
                <Route path="/about" element={<About />} loader={aboutLoader} />
            </Route>
            <Route
                path="/*"
                errorElement={<Error />}
                loader={() => {
                    throw data("Page not found", 404)
                }}
            />
        </Route>
    )
)

const rootElement = document.getElementById("root")

if (rootElement) {
    createRoot(rootElement).render(
        <StrictMode>
            <RouterProvider router={router} />
        </StrictMode>
    )
}
