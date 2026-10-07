import { Box, Typography } from "@mui/material";
import "./Footer.css";

export default function Footer() {
  return (
    <Box component="footer" className="site-footer">
      <Typography className="site-footer-brand">
        Fin<span>Hub</span>
      </Typography>
      <Typography className="site-footer-copy">
        © {new Date().getFullYear()} FinHub.
      </Typography>
    </Box>
  );
}
