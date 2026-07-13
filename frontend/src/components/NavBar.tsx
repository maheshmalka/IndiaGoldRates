import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export function NavBar() {
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate("/");
  };

  return (
    <nav className="nav-bar">
      <Link to="/" className="nav-brand">
        India Gold &amp; Silver Rates
      </Link>
      <div className="nav-links">
        {!isLoading && isAuthenticated && (
          <>
            <Link to="/rules">My alerts</Link>
            <span className="nav-user">{user?.email}</span>
            <button onClick={handleLogout}>Sign out</button>
          </>
        )}
        {!isLoading && !isAuthenticated && <Link to="/login">Sign in</Link>}
      </div>
    </nav>
  );
}
