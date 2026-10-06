import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Box,
  Button,
  Container,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import CheckCircleRounded from "@mui/icons-material/CheckCircleRounded";
import CloudUploadRounded from "@mui/icons-material/CloudUploadRounded";
import Footer from "@app/components/Footer";
import "./Home.css";

export default function Home() {
  const [sampleFile, setSampleFile] = useState("");

  return (
    <Box className="home-page">
      <Container maxWidth="xl" className="home-shell">
        <Box component="header" className="home-topbar">
          <Link to="/" className="home-brand">
            <span>
              Fin<span>Hub</span>
            </span>
          </Link>
          <Stack className="home-nav" direction="row" spacing={1.5}>
            <Button component={Link} to="/login" className="nav-login">
              Sign in
            </Button>
            <Button component={Link} to="/register" className="nav-register">
              Create account
            </Button>
          </Stack>
        </Box>

        <Box className="home-hero">
          <Box className="hero-copy">
            <Typography component="h1" className="hero-title">
              Your money
              <br />
              <em>in one place.</em>
            </Typography>
            <Typography className="hero-description">
              FinHub brings your accounts together, automatically organizes your
              transactions, and gives you simple insights to help you feel in
              control.
            </Typography>
            <Stack spacing={1.5} className="feature-list">
              <Box>
                <CheckCircleRounded />{" "}
                <span>One clear view of every account</span>
              </Box>
              <Box>
                <CheckCircleRounded /> <span>Smarter categorization</span>
              </Box>
              <Box>
                <CheckCircleRounded />{" "}
                <span>Insights that help you plan ahead</span>
              </Box>
            </Stack>
            <Paper className="import-card" elevation={0}>
              <Box className="import-icon">
                <CloudUploadRounded />
              </Box>
              <Box sx={{ flex: 1 }}>
                <Typography className="import-title">
                  Curious how it works?
                </Typography>
                <Typography className="import-copy">
                  Upload a sample statement and preview your organized finances.
                </Typography>
                {sampleFile && (
                  <Typography className="file-name">{sampleFile}</Typography>
                )}
              </Box>
              <Button
                component="label"
                className="import-button"
                endIcon={<ArrowForwardRounded />}
              >
                Try import
                <input
                  hidden
                  type="file"
                  accept=".csv,.pdf,.ofx,.qfx"
                  onChange={(event) =>
                    setSampleFile(event.target.files?.[0]?.name ?? "")
                  }
                />
              </Button>
            </Paper>
          </Box>
          <Box className="home-side">
            <Typography className="home-side-label">
              A calmer way to manage money
            </Typography>
            <Typography className="home-side-copy">
              Built for those who need a simple way to manage multiple accounts.
            </Typography>
            <Button
              component={Link}
              to="/register"
              className="primary-action home-side-button"
              endIcon={<ArrowForwardRounded />}
            >
              Start for free
            </Button>
          </Box>
        </Box>
        <Footer />
      </Container>
    </Box>
  );
}
