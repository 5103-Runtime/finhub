import { useEffect, useState } from "react";
import {
  Avatar,
  Box,
  Button,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
} from "@mui/material";
import MenuRounded from "@mui/icons-material/MenuRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import KeyboardArrowDownRounded from "@mui/icons-material/KeyboardArrowDownRounded";
import { Link, useLocation } from "react-router-dom";
import { useLogout } from "@app/hooks/useLogout";
import { getProfile, type ProfileResponse } from "@app/services/profileApi";
import "./Navbar.css";

const navigationItems = [
  { label: "Overview", href: "/overview" },
  { label: "Accounts", href: "/accounts" },
  { label: "Transactions", href: "/transactions" },
  { label: "Insights", href: "/insights" },
  { label: "Reports", href: "/reports" },
];

export default function Navbar() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileAnchor, setProfileAnchor] = useState<null | HTMLElement>(null);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const location = useLocation();
  const handleLogout = useLogout();

  useEffect(() => {
    let isCurrentRequest = true;

    getProfile()
      .then((currentProfile) => {
        if (isCurrentRequest) {
          setProfile(currentProfile);
        }
      })
      .catch((error) => {
        if (isCurrentRequest) {
          console.error("Unable to load profile for navbar:", error);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, []);

  const closeMobileMenu = () => setMobileOpen(false);
  const firstName = profile?.firstName.trim() ?? "";
  const lastName = profile?.lastName.trim() ?? "";
  const fullName = [firstName, lastName].filter(Boolean).join(" ");
  const initials = [firstName, lastName]
    .map((name) => name.charAt(0).toUpperCase())
    .join("");

  return (
    <Box component="header" className="dashboard-navbar">
      <Link
        to="/overview"
        className="dashboard-brand"
        onClick={closeMobileMenu}
      >
        Fin<span>Hub</span>
      </Link>

      <Stack component="nav" className="dashboard-nav-links" direction="row">
        {navigationItems.map((item) => (
          <Link
            key={item.href}
            to={item.href}
            className={
              location.pathname === item.href
                ? "dashboard-nav-link active"
                : "dashboard-nav-link"
            }
          >
            {item.label}
          </Link>
        ))}
      </Stack>

      <Stack className="dashboard-actions" direction="row">
        <Button
          className="dashboard-profile-trigger"
          onClick={(event) => setProfileAnchor(event.currentTarget)}
          endIcon={<KeyboardArrowDownRounded />}
          startIcon={
            <Avatar className="dashboard-avatar">
              {initials || undefined}
            </Avatar>
          }
        >
          <span className="dashboard-profile-name">{fullName}</span>
        </Button>
        <Menu
          anchorEl={profileAnchor}
          open={Boolean(profileAnchor)}
          onClose={() => setProfileAnchor(null)}
          className="dashboard-profile-menu"
        >
          <MenuItem
            component={Link}
            to="/profile"
            onClick={() => setProfileAnchor(null)}
          >
            Profile settings
          </MenuItem>
          <Divider />
          <MenuItem
            className="dashboard-signout-menu-item"
            onClick={() => {
              setProfileAnchor(null);
              void handleLogout();
            }}
          >
            Sign out
          </MenuItem>
        </Menu>
        <IconButton
          className="dashboard-menu-button"
          aria-label={mobileOpen ? "Close navigation" : "Open navigation"}
          onClick={() => {
            setProfileAnchor(null);
            setMobileOpen((open) => !open);
          }}
        >
          {mobileOpen ? <CloseRounded /> : <MenuRounded />}
        </IconButton>
      </Stack>

      {mobileOpen && (
        <Box className="dashboard-mobile-menu">
          {navigationItems.map((item) => (
            <Link
              key={item.href}
              to={item.href}
              className={
                location.pathname === item.href
                  ? "dashboard-mobile-link active"
                  : "dashboard-mobile-link"
              }
              onClick={closeMobileMenu}
            >
              {item.label}
            </Link>
          ))}
          <Divider />
          <Button
            component={Link}
            to="/profile"
            className="dashboard-mobile-link"
            onClick={closeMobileMenu}
          >
            Profile settings
          </Button>
          <Button
            className="dashboard-mobile-link dashboard-mobile-signout"
            onClick={() => {
              closeMobileMenu();
              void handleLogout();
            }}
          >
            Sign out
          </Button>
        </Box>
      )}
    </Box>
  );
}
