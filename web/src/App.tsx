import { Routes, Route } from "react-router-dom";
import Login from "@app/components/Login";
import Register from "@app/components/Register";
import Home from "@app/components/Home";
import Overview from "@app/components/Overview";
import NotFound from "@app/components/NotFound";
import SetupProfile from "@app/components/SetupProfile";
import ProtectedRoute from "@app/routes/ProtectedRoute";

function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/" element={<Home />} />
      <Route path="/home" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />

      {/* Protected Routes (requires authentication via session storage) */}
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
