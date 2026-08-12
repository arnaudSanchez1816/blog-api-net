import useAuth from "@repo/auth-provider/useAuth"
import { Navigate, Outlet, redirect } from "react-router"

export function authLoader(user: { id: string } | null) {
    const isAuthenticated = !!user
    if (!isAuthenticated) {
        throw redirect("/login")
    }
}

export interface ProtectedRouteProps {
    redirect?: string
    replace?: boolean
}

export default function ProtectedRoute({
    redirect = "/",
    replace,
}: ProtectedRouteProps) {
    const { user } = useAuth()

    if (!user) {
        return <Navigate to={redirect} replace={replace} />
    }

    return <Outlet />
}
