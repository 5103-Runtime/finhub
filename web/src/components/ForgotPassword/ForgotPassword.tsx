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
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, requestPasswordReset } from "@app/services/authApi";
import {
  Form,
  FormAlert,
  SubmitButton,
  TextFormField,
} from "@app/components/Form";
import {
  emailFormSchema,
  type EmailFormData,
} from "@app/validators/emailSchema";
import "@app/components/Login/Login.css";

export default function ForgotPassword() {
  const [serverError, setServerError] = useState("");
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);

  const form = useForm<EmailFormData>({
    resolver: zodResolver(emailFormSchema),
    defaultValues: { email: "" },
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<EmailFormData> = async (data) => {
    setServerError("");
    try {
      await requestPasswordReset({ email: data.email });
      setSubmittedEmail(data.email);
    } catch (err) {
      setServerError(
        err instanceof ApiError
          ? err.message
          : "Something went wrong. Please try again.",
      );
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
            <Typography className="auth-card-title" component="h2">
              Forgot your password?
            </Typography>
          </Box>

          {submittedEmail ? (
            <Stack spacing={2.25}>
              <FormAlert severity="success">
                If an account exists for {submittedEmail}, we've sent a link to
                reset your password. Check your inbox.
              </FormAlert>
              <Button
                component={Link}
                to="/login"
                variant="outlined"
                className="primary-action"
              >
                Back to sign in
              </Button>
            </Stack>
          ) : (
            <Form form={form} onSubmit={onSubmit}>
              <Stack spacing={2.25}>
                <Typography>
                  Enter your email and we'll send you a link to reset your
                  password.
                </Typography>
                <TextFormField<EmailFormData>
                  name="email"
                  label="Email address"
                  type="email"
                  autoComplete="email"
                />
                <FormAlert severity="error">{serverError}</FormAlert>
                <SubmitButton endIcon={<ArrowForwardRounded />}>
                  Send reset link
                </SubmitButton>
              </Stack>
            </Form>
          )}
        </Paper>
      </Container>
    </Box>
  );
}
