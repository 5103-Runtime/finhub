import { Button, Container, Stack, TextField, Typography } from "@mui/material";

export default function Login(){
    return <Container id ="page-container">
        <Stack spacing={4}>
            <Typography variant="h1" align="center">Login</Typography>
            <TextField id="email" label="Email Address" required type="email" autoComplete="email"></TextField>
            <TextField id="password" label= "Password" required type="password" autoComplete="password"></TextField>
            <Button id= "login" variant="contained">Login</Button>
        </Stack>
        <Stack sx={{marginTop: 2}} spacing={2}>
            {/*  */}
            <Typography align="center">Don't have an account?</Typography>
            <Button id= "register" variant ="contained">Register</Button>
        </Stack>
    </Container>
}