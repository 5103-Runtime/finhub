import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Container,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, requestPasswordReset } from "@app/services/authApi";
import z from "zod";
import "@app/components/Login/Login.css";

const forgotPasswordSchema = z.object({
  email: z
    .email("Enter a valid email address")
    .max(254, "Email must be 254 characters or less"),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export default function ForgotPassword() {
  const [serverError, setServerError] = useState("");
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);
  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit: SubmitHandler<ForgotPasswordFormData> = async (data) => {
    setServerError("");
    try {
      await requestPasswordReset({ email: data.email });
      setSubmittedEmail(data.email);
    } catch (err) {
      if (err instanceof ApiError) {
        setServerError(err.message);
      } else {
        setServerError("Something went wrong. Please try again.");
      }
    }
  };

  return (
    <Box className="auth-page">
      <Container maxWidth="sm" className="auth-shell">
        <Box component="header" className="auth-topbar">
          <Link to="/" className="auth-brand">
            <span>
              Fin<span>Hub</span>
            </span>
          </Link>
          <Button
            component={Link}
            to="/login"
            className="auth-back"
            startIcon={<ArrowBackRounded />}
          >
            Back to sign in
          </Button>
        </Box>
        <Paper className="auth-card auth-page-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <LockRounded />
            </Box>
            <Typography component="h2">Forgot your password?</Typography>
          </Box>

          {submittedEmail ? (
            <Stack spacing={2.25}>
                            <Alert
                className="auth-success"
                severity="success"
                variant="outlined"
                role="status"
              >
                <Typography className="auth-success-message">
                  If an account exists for {submittedEmail}, we've sent a link
                  to reset your password. Check your inbox.
                </Typography>
              </Alert>
              <Button
                component={Link}
                to="/login"
                variant="outlined"
                className="secondary-action"
              >
                Back to sign in
              </Button>
            </Stack>
          ) : (
            <Stack
              component="form"
              spacing={2.25}
              onSubmit={handleSubmit(onSubmit)}
              noValidate
            >
              <Typography>
                Enter your email and we'll send you a link to reset your
                password.
              </Typography>
              <TextField
                id="email"
                label="Email address"
                type="email"
                autoComplete="email"
                error={!!errors.email}
                helperText={errors.email?.message}
                {...registerField("email")}
              />
              {serverError && (
                <Alert
                  className="auth-error"
                  severity="error"
                  variant="outlined"
                  role="alert"
                >
                  <Typography className="auth-error-message">
                    {serverError}
                  </Typography>
                </Alert>
              )}
              <Button
                type="submit"
                variant="contained"
                className="primary-action"
                loading={isSubmitting}
                endIcon={<ArrowForwardRounded />}
              >
                Send reset link
              </Button>
            </Stack>
          )}
        </Paper>
      </Container>
    </Box>
  );
}