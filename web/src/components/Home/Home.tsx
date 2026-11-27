import { useState, type ChangeEvent } from "react";
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
import UploadFileRounded from "@mui/icons-material/UploadFileRounded";
import Footer from "@app/components/Footer";
import StatementPreviewDialog from "@app/components/StatementPreviewDialog";
import { FormAlert } from "@app/components/Form/FormAlert";
import {
  previewStatement,
  type PreviewStatementResponse,
} from "@app/services/statementApi";
import { getErrorMessage } from "@app/utils/formErrors";
import { STATEMENT_ACCEPT, validateStatementFile } from "@app/utils/statements";
import "./Home.css";

export default function Home() {
  const [preview, setPreview] = useState<PreviewStatementResponse | null>(null);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [uploadError, setUploadError] = useState("");

  const handlePreviewClose = () => {
    setPreview(null);
    setUploadError("");
  };

  const handleSampleFileChange = async (
    event: ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }

    const validationError = validateStatementFile(file);
    if (validationError) {
      setUploadError(validationError);
      setPreview(null);
      return;
    }

    setUploadError("");
    setIsPreviewing(true);
    try {
      setPreview(await previewStatement(file));
    } catch (error) {
      setPreview(null);
      setUploadError(
        getErrorMessage(error, "Unable to preview the statement."),
      );
    } finally {
      setIsPreviewing(false);
    }
  };

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
              <Box className="import-copy-block" sx={{ flex: 1 }}>
                <Typography className="import-title">
                  Curious how it works?
                </Typography>
                <Typography className="import-copy">
                  Upload a sample statement and preview how it gets parsed.
                </Typography>
              </Box>
              <Button
                component="label"
                className="import-button"
                startIcon={<UploadFileRounded />}
                loading={isPreviewing}
              >
                {isPreviewing ? "Reading statement..." : "Upload statement"}
                <input
                  hidden
                  type="file"
                  accept={STATEMENT_ACCEPT}
                  onChange={handleSampleFileChange}
                />
              </Button>
            </Paper>
            {uploadError && (
              <Box className="home-import-error">
                <FormAlert severity="error">{uploadError}</FormAlert>
              </Box>
            )}
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
      {preview && (
        <StatementPreviewDialog
          preview={preview}
          onClose={handlePreviewClose}
        />
      )}
    </Box>
  );
}
