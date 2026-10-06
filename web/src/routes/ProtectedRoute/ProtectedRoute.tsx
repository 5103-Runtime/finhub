import { Navigate, Outlet } from "react-router-dom";
import { useEffect, useState } from "react";
import { getSession } from "@app/services/authApi";

export default function ProtectedRoute() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    getSession()
      .then(() => setIsAuthenticated(true))
      .catch(() => setIsAuthenticated(false));
  }, []);

  if (isAuthenticated === null) {
    return null;
  }

  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
