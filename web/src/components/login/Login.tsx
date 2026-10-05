import { Button, Container, Stack, TextField, Typography, InputAdornment, IconButton} from "@mui/material";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import { useState } from "react";

export default function Login(){
    const [showPassword, setShowPassword] = useState(false);


    


    return <Container id ="page-container">
        <Stack spacing={4}>
            <Typography variant="h1" align="center">Login</Typography>
            <TextField id="email" label="Email Address" required type="email" autoComplete="email"></TextField>
            <TextField 
                id="password" 
                label= "Password" 
                required 
                type={showPassword ? "text" : "password"}
                autoComplete="password"
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
            <Button id= "login" variant="contained">Login</Button>
        </Stack>
        <Stack sx={{marginTop: 2}} spacing={2}>
            {/*  */}
            <Typography align="center">Don't have an account?</Typography>
            <Button id= "register" variant ="contained">Register</Button>
        </Stack>
    </Container>
}