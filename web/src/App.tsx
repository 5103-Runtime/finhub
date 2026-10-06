import { Route, Routes } from "react-router-dom";
import Login from "@app/components/Login";
import Register from "@app/components/Register";
import Home from "@app/components/Home";
import NotFound from "@app/components/NotFound";

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

export default App;
