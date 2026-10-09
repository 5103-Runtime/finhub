import { useNavigate } from "react-router-dom";
import { ApiError } from "@app/services/apiClient";
import { logout } from "@app/services/authApi";

/**
 * A custom hook for handling user logout.
 * @returns A function that performs the logout action.
 */
export function useLogout() {
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
    } catch (err) {
      if (err instanceof ApiError) {
        console.error("Logout failed:", {
          message: err.message,
          status: err.status,
          errorCode: err.errorCode,
          fieldErrors: err.fieldErrors,
        });
      } else {
        console.error("Unexpected logout error:", err);
      }
    } finally {
      navigate("/login", { replace: true });
    }
  };

  return handleLogout;
}
