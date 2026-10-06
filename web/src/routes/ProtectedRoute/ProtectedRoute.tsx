import { Navigate, Outlet } from "react-router-dom";
import { getAuthSession } from "@app/services/authStorage";

export default function ProtectedRoute() {
  const session = getAuthSession();
  const isAuthenticated = Boolean(session && session.accessToken);

  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
