import { useEffect, useState } from "react";
import {
  Box,
  Button,
  Container,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import Collapse from "@mui/material/Collapse";
import PersonOutlineRounded from "@mui/icons-material/PersonOutlineRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import ExpandMoreRounded from "@mui/icons-material/ExpandMoreRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@app/services/apiClient";
import { updatePassword } from "@app/services/authApi";
import { getProfile, updateProfile } from "@app/services/profileApi";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import {
  Form,
  FormAlert,
  PasswordField,
  SubmitButton,
  TextFormField,
} from "@app/components/Form";
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

export default function Profile() {
  const [profileError, setProfileError] = useState("");
  const [profileMessage, setProfileMessage] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [passwordMessage, setPasswordMessage] = useState("");
  const [isPasswordSectionOpen, setIsPasswordSectionOpen] = useState(false);

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

  const { reset: resetProfile } = profileForm;

  useEffect(() => {
    getProfile()
      .then((profile) =>
        resetProfile({
          ...profile,
          preferredName: profile.preferredName ?? "",
        }),
      )
      .catch((err) => {
        console.error("Profile retrieval failed:", err);
        setProfileError(
          err instanceof ApiError
            ? err.message
            : "Failed to load your profile.",
        );
      });
  }, [resetProfile]);

  const onProfileSubmit: SubmitHandler<ProfileFormData> = async (data) => {
    setProfileMessage("");
    setProfileError("");
    try {
      await updateProfile({
        ...data,
        preferredName: data.preferredName || undefined,
      });
      setProfileMessage("Your profile has been updated.");
    } catch (err) {
      console.error("Profile update failed:", err);
      setProfileError(
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
      <Container maxWidth="xl" className="auth-shell profile-shell">
        <Navbar />

        <Paper className="auth-card profile-card profile-content" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <PersonOutlineRounded />
            </Box>
            <Typography className="auth-card-title" component="h2">
              Your profile
            </Typography>
          </Box>

          <Form form={profileForm} onSubmit={onProfileSubmit}>
            <Stack spacing={2}>
              <TextFormField<ProfileFormData>
                name="firstName"
                label="First name"
                required
              />
              <TextFormField<ProfileFormData>
                name="lastName"
                label="Last name"
                required
              />
              <TextFormField<ProfileFormData>
                name="preferredName"
                label="Preferred name (optional)"
              />
              <TextFormField<ProfileFormData>
                name="dateOfBirth"
                label="Date of birth"
                type="date"
                required
                slotProps={{
                  htmlInput: {
                    min: minimumDateOfBirth,
                    max: maximumDateOfBirth,
                  },
                  inputLabel: { shrink: true },
                }}
              />
              <FormAlert severity="error">{profileError}</FormAlert>
              <FormAlert severity="success">{profileMessage}</FormAlert>
              <SubmitButton>Save profile</SubmitButton>
            </Stack>
          </Form>
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
            <Form
              form={passwordForm}
              onSubmit={onPasswordSubmit}
              className="password-form"
            >
              <Stack spacing={2}>
                <PasswordField<PasswordFormData>
                  name="currentPassword"
                  label="Current password"
                  placeholder="Enter your current password"
                  autoComplete="current-password"
                />
                <PasswordField<PasswordFormData>
                  name="password"
                  label="New password"
                  placeholder="Enter a new password"
                  autoComplete="new-password"
                />
                <PasswordField<PasswordFormData>
                  name="confirmPassword"
                  label="Confirm new password"
                  placeholder="Re-enter your new password"
                  autoComplete="new-password"
                />
                <FormAlert severity="error">{passwordError}</FormAlert>
                <FormAlert severity="success">{passwordMessage}</FormAlert>
                <SubmitButton>Update password</SubmitButton>
              </Stack>
            </Form>
          </Collapse>
        </Paper>

        <Footer />
      </Container>
    </Box>
  );
}
