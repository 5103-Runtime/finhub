// Contributor: Marlon Gonzalez
import {
  Box,
  Button,
  Container,
  Stack,
  TextField,
  Typography,
  InputAdornment,
  IconButton,
  Paper,
  Alert,
} from "@mui/material";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import { useForm, Controller, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { register, ApiError } from "@app/services/authApi";
import { saveAuthSession } from "@app/services/authStorage";
import { useState } from "react";
import z from "zod";
import { Link } from "react-router-dom";
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import "./Register.css";

// Zod schema used to validate the information included in the register form.
const signupSchema = z
  .object({
    email: z
      .email("Please enter a valid email address.")
      .max(254, "Email must be 254 characters or less."),
    password: z
      .string()
      .min(8, "Password must be at least 8 characters long.")
      .max(128, "Password must be 128 characters or less.")
      .regex(/[A-Z]/, "Password must contain at least one uppercase letter.")
      .regex(/[a-z]/, "Password must contain at least one lowercase letter.")
      .regex(/[0-9]/, "Password must contain at least one number.")
      .regex(
        /[^A-Za-z0-9]/,
        "Password must contain at least one special character.",
      ),
    confirmPassword: z.string().min(1, "Please re-enter your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });
//Defining a type based of the signup schema
type FormValues = z.infer<typeof signupSchema>;

// Functional component with a form to take in user info and validate said info using zod and react-form-hook.
function Register() {
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [serverError, setServerError] = useState("");

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(signupSchema),
    defaultValues: {
      email: "",
      password: "",
      confirmPassword: "",
    },
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<FormValues> = async (data) => {
    setServerError("");
    try {
      const response = await register({
        email: data.email,
        password: data.password,
      });

      saveAuthSession(response);
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
            <Typography component="h2">Create your account</Typography>
          </Box>
          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack spacing={2}>
              <Controller
                name="email"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="email"
                    label="Email"
                    required
                    type="email"
                    variant="outlined"
                    autoComplete="email"
                    error={Boolean(errors.email)}
                    helperText={errors.email?.message}
                  />
                )}
              />
              <Controller
                name="password"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="password"
                    label="Password"
                    required
                    type={showPassword ? "text" : "password"}
                    variant="outlined"
                    autoComplete="new-password"
                    error={Boolean(errors.password)}
                    helperText={errors.password?.message}
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
                              {showPassword ? (
                                <VisibilityOff />
                              ) : (
                                <Visibility />
                              )}
                            </IconButton>
                          </InputAdornment>
                        ),
                      },
                    }}
                  />
                )}
              />
              <Controller
                name="confirmPassword"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="confirm-password"
                    label="Re-enter Password"
                    required
                    type={showConfirmPassword ? "text" : "password"}
                    variant="outlined"
                    autoComplete="new-password"
                    error={Boolean(errors.confirmPassword)}
                    helperText={errors.confirmPassword?.message}
                    slotProps={{
                      input: {
                        endAdornment: (
                          <InputAdornment position="end">
                            <IconButton
                              onClick={() =>
                                setShowConfirmPassword((prev) => !prev)
                              }
                              edge="end"
                              aria-label={
                                showConfirmPassword
                                  ? "Hide password"
                                  : "Show password"
                              }
                            >
                              {showConfirmPassword ? (
                                <VisibilityOff />
                              ) : (
                                <Visibility />
                              )}
                            </IconButton>
                          </InputAdornment>
                        ),
                      },
                    }}
                  />
                )}
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
                id="register-btn"
                type="submit"
                loading={isSubmitting}
                variant="contained"
                className="primary-action"
                endIcon={<ArrowForwardRounded />}
              >
                Create account
              </Button>
            </Stack>
            <Stack sx={{ marginTop: 3 }} spacing={1.5}>
              <Box className="auth-divider">
                <span>Already have an account?</span>
              </Box>
              <Button
                id="register"
                component={Link}
                to="/login"
                variant="outlined"
                className="secondary-action"
              >
                Sign in
              </Button>
            </Stack>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}

export default Register;
