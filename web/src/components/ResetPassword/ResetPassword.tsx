import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Container,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError, resetPassword } from "@app/services/authApi";
import z from "zod";
import "@app/components/Login/Login.css";
import { newPasswordSchema } from "@app/validators/passwordSchemas";

const resetPasswordSchema = z
  .object({
    password: newPasswordSchema,
    confirmPassword: z.string().min(1, "Please re-enter your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

/** Reads the recovery token Supabase puts in the URL fragment. */
function readRecoveryToken(): string | null {
  const params = new URLSearchParams(window.location.hash.replace(/^#/, ""));
  if (params.get("error") || params.get("type") !== "recovery") {
    return null;
  }
  return params.get("access_token");
}

export default function ResetPassword() {
  const [accessToken] = useState(readRecoveryToken);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [serverError, setServerError] = useState("");
  const [done, setDone] = useState(false);

  // Remove the token from the address bar so it isn't kept in history.
  useEffect(() => {
    if (window.location.hash) {
      window.history.replaceState(
        window.history.state,
        "",
        window.location.pathname,
      );
    }
  }, []);

  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<ResetPasswordFormData> = async (data) => {
    if (!accessToken) return;
    setServerError("");
    try {
      await resetPassword({ accessToken, newPassword: data.password });
      setDone(true);
    } catch (err) {
      if (err instanceof ApiError) {
        console.error("Password reset failed:", {
          message: err.message,
          status: err.status,
          errorCode: err.errorCode,
        });
        setServerError(err.message);
      } else {
        console.error("Unexpected password reset error:", err);
        setServerError("Something went wrong. Please try again.");
      }
    }
  };

  let content;
  if (!accessToken) {
    content = (
      <Stack spacing={2.25}>
        <Alert severity="error" variant="outlined" role="alert">
          This reset link is invalid or has expired.
        </Alert>
        <Button
          component={Link}
          to="/forgot-password"
          variant="contained"
          className="primary-action"
        >
          Request a new link
        </Button>
      </Stack>
    );
  } else if (done) {
    content = (
      <Stack spacing={2.25}>
        <Alert
          className="auth-success"
          severity="success"
          variant="outlined"
          role="status"
        >
          <Typography className="auth-success-message">
            Your password has been updated. You can now sign in.
          </Typography>
        </Alert>
        <Button
          component={Link}
          to="/login"
          variant="contained"
          className="primary-action"
        >
          Go to sign in
        </Button>
      </Stack>
    );
  } else {
    content = (
      <Stack
        component="form"
        spacing={2.25}
        onSubmit={handleSubmit(onSubmit)}
        noValidate
      >
        <TextField
          id="password"
          label="New password"
          type={showPassword ? "text" : "password"}
          autoComplete="new-password"
          error={!!errors.password}
          helperText={errors.password?.message}
          {...registerField("password")}
          slotProps={{
            input: {
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    onClick={() => setShowPassword((prev) => !prev)}
                    edge="end"
                    aria-label={
                      showPassword ? "Hide password" : "Show password"
                    }
                  >
                    {showPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            },
          }}
        />
        <TextField
          id="confirmPassword"
          label="Confirm new password"
          type={showConfirmPassword ? "text" : "password"}
          autoComplete="new-password"
          error={!!errors.confirmPassword}
          helperText={errors.confirmPassword?.message}
          {...registerField("confirmPassword")}
          slotProps={{
            input: {
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    onClick={() => setShowConfirmPassword((prev) => !prev)}
                    edge="end"
                    aria-label={
                      showConfirmPassword
                        ? "Hide confirmation password"
                        : "Show confirmation password"
                    }
                  >
                    {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            },
          }}
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
          Update password
        </Button>
      </Stack>
    );
  }

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
            <Typography className="auth-card-title" component="h2">Set a new password</Typography>
          </Box>
          {content}
        </Paper>
      </Container>
    </Box>
  );
}