import { StrictMode, useMemo } from "react"
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
import { Spinner } from "@heroui/react"
import Login from "./pages/Login"
import ProtectedRoute, { authLoader } from "./components/ProtectedRoute"
import Error from "@repo/ui/components/Error"
import Home from "./pages/Home"
import AllPosts from "./pages/AllPosts"
import Post, { postLoader } from "./pages/Post"
import { AuthProvider } from "@repo/auth-provider/AuthProvider"
import useAuth from "@repo/auth-provider/useAuth"
import { postsAction } from "./actions/posts"
import Tags, { tagsLoader } from "./pages/Tags"
import { tagsAction } from "./actions/tags"
import { commentsAction } from "./actions/comments"
import EditPost, { editPostLoader } from "./pages/EditPost"
import { editPostsActions } from "./actions/editPosts"
import Search, { searchLoader } from "@repo/ui/components/Search/Search"
import SearchLayout, {
    searchLayoutLoader,
} from "@repo/ui/components/layouts/SearchLayout"

function Root() {
    const { user, logout, accessToken } = useAuth()

    const router = useMemo(
        () =>
            createBrowserRouter(
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
                            <Route
                                element={
                                    <ProtectedRoute redirect="/login" replace />
                                }
                                loader={() => authLoader(user)} // This should make sure that the access token/user infos are not null
                            >
                                <Route
                                    element={<SearchLayout />}
                                    loader={searchLayoutLoader}
                                >
                                    <Route index element={<Home />}></Route>
                                    <Route
                                        path="/posts"
                                        action={(actionFuncArgs) =>
                                            postsAction(
                                                actionFuncArgs,
                                                accessToken!
                                            )
                                        }
                                    >
                                        <Route
                                            index
                                            element={<AllPosts />}
                                            handle={{
                                                title: "All posts",
                                            }}
                                        />
                                        <Route
                                            path=":postSlug"
                                            element={<Post />}
                                            loader={(loaderArgs) =>
                                                postLoader(
                                                    loaderArgs,
                                                    accessToken!
                                                )
                                            }
                                            action={(loaderArgs) =>
                                                postsAction(
                                                    loaderArgs,
                                                    accessToken!
                                                )
                                            }
                                        />
                                    </Route>
                                    <Route
                                        path="/tags"
                                        element={<Tags />}
                                        loader={tagsLoader}
                                        action={(actionFuncArgs) =>
                                            tagsAction(
                                                actionFuncArgs,
                                                accessToken!
                                            )
                                        }
                                    ></Route>
                                    <Route
                                        path="/search"
                                        element={<Search />}
                                        loader={searchLoader}
                                        handle={{
                                            title: "Search",
                                        }}
                                    />
                                </Route>
                                <Route
                                    path="/posts/:postSlug/edit"
                                    element={<EditPost />}
                                    loader={(actionFuncArgs) =>
                                        editPostLoader(
                                            actionFuncArgs,
                                            accessToken!
                                        )
                                    }
                                    action={(actionsArgs) =>
                                        editPostsActions(
                                            actionsArgs,
                                            accessToken!
                                        )
                                    }
                                />
                            </Route>
                            <Route path="/login" element={<Login />}></Route>
                            <Route path="/logout" action={logout}></Route>
                            <Route
                                path="/comments/:id"
                                action={(actionArgs) =>
                                    commentsAction(actionArgs, accessToken!)
                                }
                            ></Route>
                            <Route
                                path="/*"
                                loader={() => {
                                    throw data("Page not found", 404)
                                }}
                            />
                        </Route>
                    </Route>
                )
            ),
        [accessToken, logout, user]
    )

    return <RouterProvider router={router} />
}

const rootElement = document.getElementById("root")

if (rootElement) {
    createRoot(rootElement).render(
        <StrictMode>
            <AuthProvider
                loaderComponent={
                    <div className="flex h-screen items-center justify-center">
                        <Spinner />
                    </div>
                }
            >
                <Root />
            </AuthProvider>
        </StrictMode>
    )
}
