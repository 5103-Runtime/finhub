import { useEffect, useState, type ComponentProps } from "react";
import { Link } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Container,
  InputAdornment,
  IconButton,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import ArrowBackRounded from "@mui/icons-material/ArrowBackRounded";
import PersonOutlineRounded from "@mui/icons-material/PersonOutlineRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import { useForm, useWatch, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import Collapse from "@mui/material/Collapse";
import ExpandMoreRounded from "@mui/icons-material/ExpandMoreRounded";
import { ApiError } from "@app/services/apiClient";
import { updatePassword } from "@app/services/authApi";
import { getProfile, updateProfile } from "@app/services/profileApi";
import { useLogout } from "@app/hooks/useLogout";
import {
  maximumDateOfBirth,
  minimumDateOfBirth,
  profileSchema,
  type ProfileFormData,
} from "@app/validators/profileSchemas";
import {
  passwordSchema,
  type PasswordFormData,
} from "@app/validators/passwordSchemas";
import "./Profile.css";

function PasswordField({
  id,
  label,
  show,
  onToggle,
  error,
  helperText,
  ...props
}: {
  id: string;
  label: string;
  show: boolean;
  onToggle: () => void;
  error?: boolean;
  helperText?: string;
} & ComponentProps<typeof TextField>) {
  return (
    <TextField
      {...props}
      id={id}
      label={label}
      placeholder={
        id === "current-password"
          ? "Enter your current password"
          : id === "new-password"
            ? "Enter a new password"
            : "Re-enter your new password"
      }
      type={show ? "text" : "password"}
      error={error}
      helperText={helperText}
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                onClick={onToggle}
                edge="end"
                aria-label={show ? "Hide password" : "Show password"}
              >
                {show ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        },
      }}
    />
  );
}

export default function Profile() {
  const [loadError, setLoadError] = useState("");
  const [profileMessage, setProfileMessage] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [passwordMessage, setPasswordMessage] = useState("");
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isPasswordSectionOpen, setIsPasswordSectionOpen] = useState(false);
  const handleLogout = useLogout();

  const profileForm = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      preferredName: "",
      dateOfBirth: "",
    },
    mode: "onTouched",
  });
  const passwordForm = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
    defaultValues: { currentPassword: "", password: "", confirmPassword: "" },
    mode: "onTouched",
  });
  const profileValues = useWatch({ control: profileForm.control });
  const { reset: resetProfile } = profileForm;

  useEffect(() => {
    getProfile()
      .then((profile) => resetProfile(profile))
      .catch((err) => {
        console.error("Profile retrieval failed:", err);
        setLoadError(
          err instanceof ApiError
            ? err.message
            : "Failed to load your profile.",
        );
      });
  }, [resetProfile]);

  const onProfileSubmit: SubmitHandler<ProfileFormData> = async (data) => {
    setProfileMessage("");
    setLoadError("");
    try {
      await updateProfile({
        ...data,
        preferredName: data.preferredName || undefined,
      });
      setProfileMessage("Your profile has been updated.");
    } catch (err) {
      console.error("Profile update failed:", err);
      setLoadError(
        err instanceof ApiError
          ? err.message
          : "Failed to update your profile.",
      );
    }
  };

  const onPasswordSubmit: SubmitHandler<PasswordFormData> = async (data) => {
    setPasswordError("");
    setPasswordMessage("");
    try {
      await updatePassword({
        currentPassword: data.currentPassword,
        password: data.password,
      });
      passwordForm.reset();
      setPasswordMessage("Your password has been updated successfully.");
    } catch (err) {
      console.error("Password update failed:", err);
      setPasswordError(
        err instanceof ApiError
          ? err.message
          : "Failed to update your password.",
      );
    }
  };

  return (
    <Box className="auth-page">
      <Container maxWidth="md" className="auth-shell profile-shell">
        <Box component="header" className="auth-topbar">
          <Link to="/overview" className="auth-brand">
            <span>
              Fin<span>Hub</span>
            </span>
          </Link>
          <Stack direction="row" spacing={1}>
            <Button
              component={Link}
              to="/overview"
              className="auth-back"
              startIcon={<ArrowBackRounded />}
            >
              Overview
            </Button>
            <Button
              type="button"
              variant="outlined"
              className="auth-logout-button"
              onClick={handleLogout}
            >
              Sign out
            </Button>
          </Stack>
        </Box>

        <Paper className="auth-card profile-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <PersonOutlineRounded />
            </Box>
            <Typography className="auth-card-title" component="h2">
              Your profile
            </Typography>
          </Box>
          <Stack
            component="form"
            spacing={2}
            onSubmit={profileForm.handleSubmit(onProfileSubmit)}
            noValidate
          >
            <TextField
              label="First name"
              required
              {...profileForm.register("firstName")}
              slotProps={{
                inputLabel: { shrink: Boolean(profileValues.firstName) },
              }}
              error={Boolean(profileForm.formState.errors.firstName)}
              helperText={profileForm.formState.errors.firstName?.message}
            />
            <TextField
              label="Last name"
              required
              {...profileForm.register("lastName")}
              slotProps={{
                inputLabel: { shrink: Boolean(profileValues.lastName) },
              }}
              error={Boolean(profileForm.formState.errors.lastName)}
              helperText={profileForm.formState.errors.lastName?.message}
            />
            <TextField
              label="Preferred name (optional)"
              {...profileForm.register("preferredName")}
              slotProps={{
                inputLabel: { shrink: Boolean(profileValues.preferredName) },
              }}
              error={Boolean(profileForm.formState.errors.preferredName)}
              helperText={profileForm.formState.errors.preferredName?.message}
            />
            <TextField
              label="Date of birth"
              type="date"
              required
              {...profileForm.register("dateOfBirth")}
              slotProps={{
                htmlInput: { min: minimumDateOfBirth, max: maximumDateOfBirth },
                inputLabel: { shrink: Boolean(profileValues.dateOfBirth) },
              }}
              error={Boolean(profileForm.formState.errors.dateOfBirth)}
              helperText={profileForm.formState.errors.dateOfBirth?.message}
            />
            {loadError && (
              <Alert
                className="auth-error"
                severity="error"
                variant="outlined"
                role="alert"
              >
                <Typography className="auth-error-message">
                  {loadError}
                </Typography>
              </Alert>
            )}
            {profileMessage && (
              <Alert
                className="auth-success"
                severity="success"
                variant="outlined"
                role="status"
              >
                <Typography className="auth-success-message">
                  {profileMessage}
                </Typography>
              </Alert>
            )}
            <Button
              type="submit"
              loading={profileForm.formState.isSubmitting}
              variant="contained"
              className="primary-action"
            >
              Save profile
            </Button>
          </Stack>
        </Paper>

        <Paper className="auth-card profile-card password-card" elevation={0}>
          <Button
            className="password-section-toggle"
            onClick={() => setIsPasswordSectionOpen((isOpen) => !isOpen)}
            aria-expanded={isPasswordSectionOpen}
            fullWidth
          >
            <Box className="auth-card-heading">
              <Box className="lock-icon">
                <LockRounded />
              </Box>
              <Typography className="auth-card-title" component="h2">
                Change password
              </Typography>
            </Box>
            <ExpandMoreRounded
              className={isPasswordSectionOpen ? "expanded" : ""}
            />
          </Button>
          <Collapse in={isPasswordSectionOpen}>
            <Stack
              className="password-form"
              component="form"
              spacing={2}
              onSubmit={passwordForm.handleSubmit(onPasswordSubmit)}
              noValidate
            >
              <PasswordField
                label="Current password"
                id="current-password"
                autoComplete="current-password"
                show={showCurrentPassword}
                onToggle={() => setShowCurrentPassword((value) => !value)}
                {...passwordForm.register("currentPassword")}
                error={Boolean(passwordForm.formState.errors.currentPassword)}
                helperText={
                  passwordForm.formState.errors.currentPassword?.message
                }
              />
              <PasswordField
                label="New password"
                id="new-password"
                autoComplete="new-password"
                show={showPassword}
                onToggle={() => setShowPassword((value) => !value)}
                {...passwordForm.register("password")}
                error={Boolean(passwordForm.formState.errors.password)}
                helperText={passwordForm.formState.errors.password?.message}
              />
              <PasswordField
                label="Confirm new password"
                id="confirm-password"
                autoComplete="new-password"
                show={showConfirmPassword}
                onToggle={() => setShowConfirmPassword((value) => !value)}
                {...passwordForm.register("confirmPassword")}
                error={Boolean(passwordForm.formState.errors.confirmPassword)}
                helperText={
                  passwordForm.formState.errors.confirmPassword?.message
                }
              />
              {passwordError && (
                <Alert
                  className="auth-error"
                  severity="error"
                  variant="outlined"
                  role="alert"
                >
                  <Typography className="auth-error-message">
                    {passwordError}
                  </Typography>
                </Alert>
              )}
              {passwordMessage && (
                <Alert
                  className="auth-success"
                  severity="success"
                  variant="outlined"
                  role="status"
                >
                  <Typography className="auth-success-message">
                    {passwordMessage}
                  </Typography>
                </Alert>
              )}
              <Button
                type="submit"
                loading={passwordForm.formState.isSubmitting}
                variant="contained"
                className="primary-action"
              >
                Update password
              </Button>
            </Stack>
          </Collapse>
        </Paper>
      </Container>
    </Box>
  );
}
