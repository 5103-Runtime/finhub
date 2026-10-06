import { Box, Button, Container, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { logout } from "@app/services/authApi";

// TODO: Just a placeholder for now, will be replaced with the actual overview page once we have more features implemented.
export default function Overview() {
  const navigate = useNavigate();

  const handleLogout = () => {
    void logout();
    sessionStorage.removeItem("needs_setup");
    navigate("/login");
  };

  return (
    <Box
      sx={{ minHeight: "100vh", bgcolor: "#202332", color: "#f7f4ef", p: 4 }}
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
            Fin<span style={{ color: "#bc9876" }}>Hub</span> Overview
          </Typography>
          <Button
            variant="outlined"
            onClick={handleLogout}
            sx={{ color: "#bc9876", borderColor: "#bc9876" }}
          >
            Sign out
          </Button>
        </Box>
        <Typography variant="body1" sx={{ color: "#b5bdc2" }}>
          Welcome back! You are successfully signed in and viewing your account
          overview.
        </Typography>
      </Container>
    </Box>
  );
}
