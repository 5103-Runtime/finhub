import { Box, Container } from "@mui/material";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import "./Overview.css";

export default function Overview() {
  return (
    <Box className="overview-page">
      <Container maxWidth="xl" className="overview-shell">
        <Navbar />
        <Footer />
      </Container>
    </Box>
  );
}
