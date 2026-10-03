// Contributor: Marlon Gonzalez
import { Button, Container, Stack, TextField, Typography } from "@mui/material";

function Register() {
  return <Container id="page-container">
    <Stack id="text-field-box" spacing={2}>
      <Typography variant="h1" align="center">Register</Typography>
      <TextField id="first-name" label="First Name" required variant="outlined"/>
      <TextField id="last-name" label="Last Name" required variant="outlined"/>
      <TextField id="pref-name" label="Preferred Name" required variant="outlined"/>
      <TextField id="email" label="Email" required variant="outlined"/>
      <TextField id="password" label="Password" required variant="outlined"/>
      <Button id="register-btn" variant="contained">Register</Button>
    </Stack>
  </Container>;
}

export default Register
