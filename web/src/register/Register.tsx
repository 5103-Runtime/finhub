// Contributor: Marlon Gonzalez
import { Box, Button, Container, Stack, TextField, Typography } from "@mui/material";
import { useForm, Controller, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from "@hookform/resolvers/zod";
import { register, ApiError } from "../services/authApi";
import z from "zod";

// Zod schema used to validate the information included in the register form.
const signupSchema = z.object({
  email: z
  .email("Please enter a valid email address.")
  .max(254, "Email must be 254 characters or less."),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters long.')
    .max(128, 'Password must be 128 characters or less.')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
    .regex(/[0-9]/, 'Password must contain at least one number.')
    .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character.'),
  confirmPassword: z
  .string()
  .min(1,"Please re-enter your password")
  
  ,
})
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });
//Defining a type based of the signup schema
type FormValues = z.infer<typeof signupSchema>

// Functional component with a form to take in user info and validate said info using zod and react-form-hook.
function Register() {

  const { control, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(signupSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: ''
    },
    mode: 'onTouched',
  });

  const onSubmit: SubmitHandler<FormValues> = async (data) => {
  try {
    const response = await register({
      email: data.email,
      password: data.password,
    });

    console.log("Registration successful:", response);
  } catch (err) {
    if (err instanceof ApiError) {
      console.error("Registration failed:", {
      message: err.message,
      status: err.status,
      errorCode: err.errorCode,
      fieldErrors: err.fieldErrors,
    });
    } else {
      console.error("Unexpected error:", err);
    }
  }
};

  return <Container id="page-container">
    <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate >
      <Stack id="text-field-box" spacing={2}>
        <Typography variant="h1" align="center">Register</Typography>
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
              type="password"
              variant="outlined"
              autoComplete="new-password"
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
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
              type="password"
              variant="outlined"
              autoComplete="new-password"
              error={Boolean(errors.confirmPassword)}
              helperText={errors.confirmPassword?.message}
            />
          )}
        />
        <Button id="register-btn" type="submit" variant="contained">Register</Button>
      </Stack>
    </Box>
  </Container>;
}

export default Register
