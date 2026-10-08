import { Box, Button, Container, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { ApiError, logout } from "@app/services/authApi";

// TODO: Just a placeholder for now, will be replaced with the actual overview page once we have more features implemented.
export default function Overview() {
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

  return (
    <Box
      sx={{
        minHeight: "100vh",
        bgcolor: "var(--color-ink)",
        color: "var(--color-cream)",
        p: 4,
      }}
    >
      <Container maxWidth="lg">
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            mb: 4,
          }}
        >
          <Typography variant="h4" sx={{ fontWeight: 800 }}>
            Fin<span style={{ color: "var(--color-bronze)" }}>Hub</span>{" "}
            Overview
          </Typography>
          <Button
            variant="outlined"
            onClick={handleLogout}
            sx={{
              color: "var(--color-bronze)",
              borderColor: "var(--color-bronze)",
            }}
          >
            Sign out
          </Button>
        </Box>
        <Typography variant="body1" sx={{ color: "var(--color-mist)" }}>
          Welcome back! You are successfully signed in and viewing your account
          overview.
        </Typography>
      </Container>
    </Box>
  );
}
