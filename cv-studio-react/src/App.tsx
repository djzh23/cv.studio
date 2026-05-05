import { useEffect, useState } from "react";
import { Navigate, Outlet, Route, Routes, useLocation } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { getAccessGranted } from "./lib/cvStudio";
import { AccessPage } from "./pages/AccessPage";
import { HelpPage } from "./pages/HelpPage";
import { HomePage } from "./pages/HomePage";
import { VariantsPage } from "./pages/VariantsPage";

function RequireAccess() {
  const loc = useLocation();
  const [ok, setOk] = useState<boolean | null>(null);

  useEffect(() => {
    setOk(getAccessGranted());
  }, [loc.pathname, loc.search]);

  if (ok === null) {
    return (
      <section className="empty-state">
        <h1>CvStudio</h1>
        <p>Session wird geprueft...</p>
      </section>
    );
  }

  if (!ok) {
    const returnUrl = loc.pathname + loc.search || "/";
    return <Navigate to={`/access?returnUrl=${encodeURIComponent(returnUrl)}`} replace />;
  }

  return <Outlet />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/access" element={<AccessPage />} />
      <Route element={<RequireAccess />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/arbeitsversion/:id" element={<HomePage />} />
          <Route path="/resume/:id" element={<HomePage />} />
          <Route path="/varianten" element={<VariantsPage />} />
          <Route path="/varianten/:resumeId" element={<VariantsPage />} />
          <Route path="/versions" element={<VariantsPage />} />
          <Route path="/versions/:resumeId" element={<VariantsPage />} />
          <Route path="/hilfe" element={<HelpPage />} />
          <Route path="/how-to-use" element={<HelpPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
