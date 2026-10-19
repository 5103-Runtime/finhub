import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
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
import { ApiError, login } from "@app/services/authApi";
import {
  Form,
  FormAlert,
  PasswordField,
  SubmitButton,
  TextFormField,
} from "@app/components/Form";
import { loginSchema, type LoginFormData } from "@app/validators/loginSchema";
import "./Login.css";

export default function Login() {
  const [serverError, setServerError] = useState("");
  const navigate = useNavigate();

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit: SubmitHandler<LoginFormData> = async (data) => {
    setServerError("");
    try {
      const response = await login(data);
      navigate(response.requiresProfileSetup ? "/setup-profile" : "/overview");
    } catch (err) {
      if (err instanceof ApiError) {
        console.error("Login failed:", {
          message: err.message,
          status: err.status,
          errorCode: err.errorCode,
          fieldErrors: err.fieldErrors,
        });
        setServerError(err.message);
      } else {
        console.error("Unexpected login error:", err);
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
            to="/"
            className="auth-back"
            startIcon={<ArrowBackRounded />}
          >
            Back to home
          </Button>
        </Box>

        <Paper className="auth-card auth-page-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <LockRounded />
            </Box>
            <Typography className="auth-card-title" component="h2">
              Sign in to FinHub
            </Typography>
          </Box>

          <Form form={form} onSubmit={onSubmit}>
            <Stack spacing={2.25}>
              <TextFormField<LoginFormData>
                name="email"
                label="Email address"
                type="email"
                autoComplete="email"
              />
              <PasswordField<LoginFormData>
                name="password"
                label="Password"
                autoComplete="current-password"
              />
              <Button
                component={Link}
                to="/forgot-password"
                size="small"
                className="forgot-password-link"
              >
                Forgot password?
              </Button>
              <FormAlert severity="error">{serverError}</FormAlert>
              <SubmitButton endIcon={<ArrowForwardRounded />}>
                Sign in
              </SubmitButton>
            </Stack>
          </Form>

          <Stack sx={{ marginTop: 3 }} spacing={1.5}>
            <Box className="auth-divider">
              <span>New to FinHub?</span>
            </Box>
            <Button
              component={Link}
              to="/register"
              variant="outlined"
              className="secondary-action"
            >
              Create an account
            </Button>
          </Stack>
        </Paper>
      </Container>
    </Box>
  );
}
