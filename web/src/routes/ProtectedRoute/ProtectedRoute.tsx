import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { getSession } from "@app/services/authApi";

export default function ProtectedRoute() {
  const [session, setSession] = useState<Awaited<
    ReturnType<typeof getSession>
  > | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
  const [checkedPath, setCheckedPath] = useState<string | null>(null);
  const location = useLocation();
  const pathname = location.pathname;

  useEffect(() => {
    let isCurrentRequest = true;

    getSession()
      .then((currentSession) => {
        if (!isCurrentRequest) {
          return;
        }
        setSession(currentSession);
        setIsAuthenticated(currentSession.authenticated);
        setCheckedPath(pathname);
      })
      .catch(() => {
        if (!isCurrentRequest) {
          return;
        }
        setIsAuthenticated(false);
        setCheckedPath(pathname);
      });

    return () => {
      isCurrentRequest = false;
    };
  }, [pathname]);

  if (isAuthenticated === null || checkedPath !== pathname) {
    return null;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (session?.requiresProfileSetup && location.pathname !== "/setup-profile") {
    return <Navigate to="/setup-profile" replace />;
  }

  if (
    !session?.requiresProfileSetup &&
    location.pathname === "/setup-profile"
  ) {
    return <Navigate to="/overview" replace />;
  }

  return <Outlet />;
}
