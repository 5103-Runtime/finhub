import { Link } from "react-router-dom";
import { Box, Button, Container, Typography } from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import Footer from "@app/components/Footer";
import "./NotFound.css";

export default function NotFound() {
  return (
    <Box className="not-found-page">
      <Container maxWidth="xl" className="not-found-shell">
        <Box className="not-found-content">
          <Typography className="error-code">404 Error</Typography>
          <Typography component="h1" className="error-title">
            Page not found
          </Typography>
          <Typography className="error-description">
            The page you are looking for doesn't exist or has been moved.
          </Typography>
          <Button
            component={Link}
            to="/"
            className="primary-action error-button"
            endIcon={<ArrowForwardRounded />}
          >
            Back to home
          </Button>
        </Box>

        <Footer />
      </Container>
    </Box>
  );
}
