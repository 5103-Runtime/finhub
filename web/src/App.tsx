import { Routes, Route, useLocation } from "react-router-dom";
import Login from "@app/components/Login";
import Register from "@app/components/Register";
import Home from "@app/components/Home";
import Overview from "@app/components/Overview";
import NotFound from "@app/components/NotFound";
import SetupProfile from "@app/components/SetupProfile";
import Profile from "@app/components/Profile";
import ProtectedRoute from "@app/routes/ProtectedRoute";
import PublicRoute from "@app/routes/PublicRoute";
import ForgotPassword from "@app/components/ForgotPassword";
import ResetPassword from "@app/components/ResetPassword";
import Accounts from "@app/components/Accounts";
import Transactions from "@app/components/Transactions";
import Categories from "@app/components/Categories";

function TransactionsRoute() {
  const location = useLocation();
  return <Transactions key={`${location.pathname}${location.search}`} />;
}

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
        <Route path="/reset-password" element={<ResetPassword />} />
      </Route>

      {/* Protected Routes (authentication is validated by the API cookie session) */}
      <Route element={<ProtectedRoute />}>
        <Route path="/overview" element={<Overview />} />
        <Route path="/setup-profile" element={<SetupProfile />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/accounts" element={<Accounts />} />
        <Route path="/transactions" element={<TransactionsRoute />} />
        <Route path="/categories" element={<Categories />} />
      </Route>

      {/* 404 Catch-All */}
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

export default App;
