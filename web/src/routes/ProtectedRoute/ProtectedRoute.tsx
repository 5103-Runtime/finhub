import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { getSession } from "@app/services/authApi";

export default function ProtectedRoute() {
  const [session, setSession] = useState<Awaited<
    ReturnType<typeof getSession>
  > | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
  const location = useLocation();

  useEffect(() => {
    getSession()
      .then((currentSession) => {
        setSession(currentSession);
        setIsAuthenticated(currentSession.authenticated);
      })
      .catch(() => setIsAuthenticated(false));
  }, []);

  if (isAuthenticated === null) {
    return null;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (session?.requiresProfileSetup && location.pathname !== "/setup-profile") {
    return <Navigate to="/setup-profile" replace />;
  }

  if (!session?.requiresProfileSetup && location.pathname === "/setup-profile") {
    return <Navigate to="/overview" replace />;
  }

  return <Outlet />;
}
