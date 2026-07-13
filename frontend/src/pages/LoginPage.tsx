import { useSearchParams } from "react-router-dom";
import { loginUrl } from "../api/auth";

export function LoginPage() {
  const [searchParams] = useSearchParams();
  const error = searchParams.get("error");

  return (
    <div className="login-page">
      <h1>Sign in</h1>
      <p className="disclaimer">
        Sign in to configure rate-change alerts. Viewing rates doesn't require an account.
      </p>

      {error && (
        <p className="login-error">
          Sign-in didn't complete — please try again.
        </p>
      )}

      <div className="login-buttons">
        <a className="login-button login-button--google" href={loginUrl("google")}>
          Sign in with Google
        </a>
        <a className="login-button login-button--microsoft" href={loginUrl("microsoft")}>
          Sign in with Microsoft
        </a>
      </div>
    </div>
  );
}
