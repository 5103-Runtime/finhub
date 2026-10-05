import { Button, Container, Stack, TextField, Typography, InputAdornment, IconButton} from "@mui/material";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import z from "zod";
import { Link } from "react-router-dom";

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

export default function Login(){
    const [showPassword, setShowPassword] = useState(false);
    const { register, handleSubmit, formState: { errors, isSubmitting }, } = useForm<LoginFormData>({resolver: zodResolver(loginSchema),});
    const onSubmit = async (data : LoginFormData) => {
        try {
            // TODO: put API here
        } catch (error) {
            
        }
    }

    return <Container id ="page-container">
        <Stack component="form" spacing={4} onSubmit={handleSubmit(onSubmit)} noValidate>
            <Typography variant="h1" align="center">Login</Typography>
            <TextField
                id="email"
                label="Email Address"
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
            ></TextField>
            <Button type="submit" variant="contained" loading={isSubmitting}>Login</Button>
        </Stack>
        <Stack sx={{marginTop: 2}} spacing={2}>
            <Typography align="center">Don't have an account?</Typography>
            <Button id= "register" component ={Link} to="/register" variant ="contained" >Register</Button>
        </Stack>
    </Container>
}