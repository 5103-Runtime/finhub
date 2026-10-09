import { Box, Button, Container, Typography } from "@mui/material";
import { useLogout } from "@app/hooks/useLogout";

// TODO: Just a placeholder for now, will be replaced with the actual overview page once we have more features implemented.
export default function Overview() {
  const handleLogout = useLogout();

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
            className="auth-logout-button"
            onClick={handleLogout}
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
