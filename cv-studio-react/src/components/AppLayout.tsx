import { NavLink, Outlet } from "react-router-dom";

export function AppLayout() {
  return (
    <>
      <div className="gradient-bar" />
      <header className="top-nav">
        <nav>
          <div className="nav-brand">
            <span className="nav-logo">
              CV<span>.</span>Studio
            </span>
          </div>
          <div className="nav-center">
            <NavLink to="/" className={({ isActive }) => (isActive ? "active" : undefined)} end>
              <i className="bi bi-house" /> Start
            </NavLink>
            <NavLink to="/varianten" className={({ isActive }) => (isActive ? "active" : undefined)}>
              <i className="bi bi-layers" /> Varianten
            </NavLink>
            <NavLink to="/hilfe" className={({ isActive }) => (isActive ? "active" : undefined)}>
              <i className="bi bi-question-circle" /> Hilfe
            </NavLink>
          </div>
          <div className="nav-status" aria-label="Auto-Save Status">
            <span className="nav-autosave-dot" />
            <span>Auto-Save aktiv</span>
          </div>
        </nav>
      </header>
      <div className="main-layout">
        <Outlet />
      </div>
    </>
  );
}
