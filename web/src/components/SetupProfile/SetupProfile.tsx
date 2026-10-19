import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import {
  Box,
  Button,
  Container,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import PersonOutlineRounded from "@mui/icons-material/PersonOutlineRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@app/services/apiClient";
import { useLogout } from "@app/hooks/useLogout";
import { updateProfile } from "@app/services/profileApi";
import {
  Form,
  FormAlert,
  SubmitButton,
  TextFormField,
} from "@app/components/Form";
import {
  maximumDateOfBirth,
  minimumDateOfBirth,
  profileSchema,
  type ProfileFormData,
} from "@app/validators/profileSchemas";
import "./SetupProfile.css";

export default function SetupProfile() {
  const [serverError, setServerError] = useState("");
  const navigate = useNavigate();
  const handleLogout = useLogout();

  const form = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      preferredName: "",
      dateOfBirth: "",
    },
    mode: "onTouched",
  });

  const onSubmit: SubmitHandler<ProfileFormData> = async (data) => {
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
          <Button
            type="button"
            variant="outlined"
            className="auth-logout-button"
            onClick={handleLogout}
          >
            Sign out
          </Button>
        </Box>

        <Paper className="auth-card register-card" elevation={0}>
          <Box className="auth-card-heading">
            <Box className="lock-icon">
              <PersonOutlineRounded />
            </Box>
            <Typography className="auth-card-title" component="h2">
              Complete your profile
            </Typography>
          </Box>

          <Form form={form} onSubmit={onSubmit}>
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
              <FormAlert severity="error">{serverError}</FormAlert>
              <SubmitButton endIcon={<ArrowForwardRounded />}>
                Continue
              </SubmitButton>
            </Stack>
          </Form>
        </Paper>
      </Container>
    </Box>
  );
}
