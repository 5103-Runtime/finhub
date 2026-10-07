import { Navigate, Outlet } from "react-router-dom";
import { useEffect, useState } from "react";
import { getSession } from "@app/services/authApi";

export default function PublicRoute() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
  const [requiresProfileSetup, setRequiresProfileSetup] = useState(false);

  useEffect(() => {
    getSession()
      .then((session) => {
        setRequiresProfileSetup(session.requiresProfileSetup);
        setIsAuthenticated(true);
      })
      .catch(() => setIsAuthenticated(false));
  }, []);

  if (isAuthenticated === null) {
    return null;
  }

  if (isAuthenticated) {
    return (
      <Navigate
        to={requiresProfileSetup ? "/setup-profile" : "/overview"}
        replace
      />
    );
  }

  return <Outlet />;
}
