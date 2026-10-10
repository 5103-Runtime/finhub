import { Routes, Route } from "react-router-dom";
import Login from "@app/components/Login";
import Register from "@app/components/Register";
import Home from "@app/components/Home";
import Overview from "@app/components/Overview";
import NotFound from "@app/components/NotFound";
import SetupProfile from "@app/components/SetupProfile";
import ProtectedRoute from "@app/routes/ProtectedRoute";
import PublicRoute from "@app/routes/PublicRoute";
import ForgotPassword from "@app/components/ForgotPassword";

function App() {
  return (
    <Routes>
      {/* Public routes redirect authenticated users to the correct app page. */}
      <Route element={<PublicRoute />}>
        <Route path="/" element={<Home />} />
        <Route path="/home" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
      </Route>

      {/* Protected Routes (authentication is validated by the API cookie session) */}
      <Route element={<ProtectedRoute />}>
        <Route path="/overview" element={<Overview />} />
        <Route path="/setup-profile" element={<SetupProfile />} />
      </Route>

      {/* 404 Catch-All */}
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

export default App;
