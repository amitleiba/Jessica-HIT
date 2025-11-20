import { useAppSelector } from "../store/hooks";
import { Navigate, Outlet } from "react-router-dom";

const ProtectedRoute = () => {
  const { isAuthenticated } = useAppSelector((state) => state.auth);

  if (isAuthenticated) {
    return <Outlet />;
  }
  return <Navigate to="/login" />;
};

export default ProtectedRoute;
