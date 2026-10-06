import { useState } from "react";
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
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import z from "zod";
import "./Login.css";

const loginSchema = z.object({
  email: z
    .email("Enter a valid email address")
    .max(254, "Email must be 254 characters or less"),
  password: z
    .string()
    .min(1, "Password is required")
    .max(128, "Password must be 128 characters or less"),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function Login() {
  const [showPassword, setShowPassword] = useState(false);
  const [serverError, setServerError] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async () => {
    // TODO: Replace with the authentication API when it is implemented.
    setServerError("This is to test the server error");
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
            <Typography component="h2">Sign in to FinHub</Typography>
          </Box>
          <Stack
            component="form"
            spacing={2.25}
            onSubmit={handleSubmit(onSubmit)}
            noValidate
          >
            <TextField
              id="email"
              label="Email address"
              type="email"
              autoComplete="email"
              error={!!errors.email}
              helperText={errors.email?.message}
              {...register("email")}
            />
            <TextField
              id="password"
              label="Password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              error={!!errors.password}
              helperText={errors.password?.message}
              {...register("password")}
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
            <Button
              type="submit"
              variant="contained"
              className="primary-action"
              loading={isSubmitting}
              endIcon={<ArrowForwardRounded />}
            >
              Sign in
            </Button>
            {serverError && <Alert severity="error">{serverError}</Alert>}
          </Stack>
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
