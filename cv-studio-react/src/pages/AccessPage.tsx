import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { verifyAccess } from "../api/resumeApi";
import { getAccessGranted, setAccessGranted } from "../lib/cvStudio";

function safeReturnUrl(candidate: string | null): string {
  if (!candidate?.trim()) {
    return "/";
  }
  if (candidate.startsWith("http")) {
    return "/";
  }
  return candidate.startsWith("/") ? candidate : `/${candidate}`;
}

export function AccessPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnUrl = safeReturnUrl(params.get("returnUrl"));

  const [ready, setReady] = useState(false);
  const [passcode, setPasscode] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (getAccessGranted()) {
      navigate(returnUrl, { replace: true });
      return;
    }
    setReady(true);
  }, [navigate, returnUrl]);

  const unlock = async () => {
    setError(null);
    try {
      await verifyAccess(passcode);
      setAccessGranted(true);
      navigate(returnUrl, { replace: true });
    } catch {
      setError("Ungueltiger Passcode.");
    }
  };

  if (!ready) {
    return (
      <section className="empty-state" style={{ maxWidth: 560, marginTop: "2rem" }}>
        <h1>Zugang</h1>
        <p>Lade Session...</p>
      </section>
    );
  }

  return (
    <section className="empty-state" style={{ maxWidth: 560, marginTop: "2rem" }}>
      <h1>Zugangscode</h1>
      <p>Bitte gib den Passcode ein, um CvStudio zu nutzen.</p>
      <div style={{ textAlign: "left", marginTop: "1rem" }}>
        <label htmlFor="access-passcode">Passcode</label>
        <input
          id="access-passcode"
          className="form-control"
          type="password"
          value={passcode}
          onChange={(e) => setPasscode(e.target.value)}
          autoComplete="one-time-code"
        />
      </div>
      {error ? (
        <div className="alert alert-danger" role="alert" style={{ marginTop: "1rem" }}>
          {error}
        </div>
      ) : null}
      <div className="toolbar-actions" style={{ marginTop: "1rem" }}>
        <button type="button" className="btn btn-primary-action" onClick={() => void unlock()}>
          Freischalten
        </button>
      </div>
    </section>
  );
}
