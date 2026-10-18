// Contributor: Marlon Gonzalez
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Box, Button, Container, Paper, Stack, Typography } from "@mui/material";
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, register } from "@app/services/authApi";
import { Form, FormAlert, PasswordField, SubmitButton, TextFormField} from "@app/components/Form";
import { registerSchema, type RegisterFormData,} from "@app/validators/registerSchema";
import "./Register.css";

function Register() {
  const [serverError, setServerError] = useState("");
  const navigate = useNavigate();

  const form = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: "", password: "", confirmPassword: "" },
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<RegisterFormData> = async (data) => {
    setServerError("");
    try {
      await register({
          email: data.email, 
          password: data.password,
        });
      navigate("/setup-profile");
    } catch (err) {
      if (err instanceof ApiError) {
        console.error("Registration failed:", {
          message: err.message,
          status: err.status,
          errorCode: err.errorCode,
          fieldErrors: err.fieldErrors,
        });
        setServerError(err.message);
      } else {
        setServerError("Something went wrong. Please try again.");
        console.error("Unexpected error:", err);
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
        <Paper className="auth-card register-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <LockRounded />
            </Box>
            <Typography className="auth-card-title" component="h2">
              Create your account
            </Typography>
          </Box>
          <Form form={form} onSubmit={onSubmit}>
            <Stack spacing={2.25}>
              <TextFormField<RegisterFormData>
                name="email"
                label="Email"
                type="email"
                required
                autoComplete="email"
              />
              <PasswordField<RegisterFormData>
                name="password"
                label="Password"
                required
                autoComplete="new-password"
              />
              <PasswordField<RegisterFormData>
                name="confirmPassword"
                label="Re-enter Password"
                required
                autoComplete="new-password"
              />
              <FormAlert severity="error">{serverError}</FormAlert>
              <SubmitButton endIcon={<ArrowForwardRounded />}>
                Create account
              </SubmitButton>
            </Stack>
          </Form>

          <Stack sx={{ marginTop: 3 }} spacing={1.5}>
            <Box className="auth-divider">
              <span>Already have an account?</span>
            </Box>
            <Button
              component={Link}
              to="/login"
              variant="outlined"
              className="secondary-action"
            >
              Sign in
            </Button>
          </Stack>
        </Paper>
      </Container>
    </Box>
  );
}

export default Register;