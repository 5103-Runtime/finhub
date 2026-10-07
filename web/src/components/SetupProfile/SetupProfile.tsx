import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
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
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import PersonOutlineRounded from "@mui/icons-material/PersonOutlineRounded";
import { useForm, Controller, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@app/services/apiClient";
import { updateProfile } from "@app/services/profileApi";
import z from "zod";
import "./SetupProfile.css";

const setupSchema = z.object({
  firstName: z
    .string()
    .min(1, "First name is required")
    .max(50, "Must be 50 characters or less"),
  lastName: z
    .string()
    .min(1, "Last name is required")
    .max(50, "Must be 50 characters or less"),
  preferredName: z.string().max(50, "Must be 50 characters or less").optional(),
  dateOfBirth: z.string().min(1, "Date of birth is required"),
});

type SetupFormData = z.infer<typeof setupSchema>;

export default function SetupProfile() {
  const [serverError, setServerError] = useState("");
  const navigate = useNavigate();

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SetupFormData>({
    resolver: zodResolver(setupSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      preferredName: "",
      dateOfBirth: "",
    },
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<SetupFormData> = async (data) => {
    setServerError("");
    try {
      await updateProfile({
        ...data,
        preferredName: data.preferredName || undefined,
      });
      navigate("/overview", { replace: true });
    } catch (err) {
      console.error("Profile setup failed:", err);
      setServerError(
        err instanceof ApiError
          ? err.message
          : "Failed to save your profile details. Please try again.",
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
        </Box>
        <Paper className="auth-card register-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <PersonOutlineRounded />
            </Box>
            <Typography component="h2">Complete your profile</Typography>
          </Box>
          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack spacing={2}>
              <Controller
                name="firstName"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="firstName"
                    label="First name"
                    required
                    variant="outlined"
                    error={Boolean(errors.firstName)}
                    helperText={errors.firstName?.message}
                  />
                )}
              />
              <Controller
                name="lastName"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="lastName"
                    label="Last name"
                    required
                    variant="outlined"
                    error={Boolean(errors.lastName)}
                    helperText={errors.lastName?.message}
                  />
                )}
              />
              <Controller
                name="preferredName"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="preferredName"
                    label="Preferred name (optional)"
                    variant="outlined"
                    error={Boolean(errors.preferredName)}
                    helperText={errors.preferredName?.message}
                  />
                )}
              />
              <Controller
                name="dateOfBirth"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    id="dateOfBirth"
                    label="Date of birth"
                    type="date"
                    required
                    variant="outlined"
                    slotProps={{
                      inputLabel: { shrink: true },
                    }}
                    error={Boolean(errors.dateOfBirth)}
                    helperText={errors.dateOfBirth?.message}
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
                id="setup-submit-btn"
                type="submit"
                loading={isSubmitting}
                variant="contained"
                className="primary-action"
                endIcon={<ArrowForwardRounded />}
              >
                Continue
              </Button>
            </Stack>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
}
