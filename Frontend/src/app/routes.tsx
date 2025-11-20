import { Routes, Route, Navigate } from "react-router-dom";
import Login from "../pages/Login";
import Register from "../pages/Register";
import UserSettings from "../pages/UserSettings";
import Home from "../pages/Home";
import ProtectedRoute from "./guards/ProtectedRoute";

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/home" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/" element={<Navigate to="/home" replace />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/user-settings" element={<UserSettings />} />
      </Route>
      <Route
        path="*"
        element={
          <div style={{ padding: "2rem" }}>
            <h1>404 - Page Not Found</h1>
            <p>The page you're looking for doesn't exist.</p>
          </div>
        }
      />
    </Routes>
  );
};

export default AppRoutes;
