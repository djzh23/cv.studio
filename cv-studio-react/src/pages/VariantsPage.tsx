import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import * as api from "../api/resumeApi";
import { downloadBlob, getLastResumeId, notify, setLastResumeId } from "../lib/cvStudio";
import { versionBadgeClass } from "../lib/formatting";
import type { PdfDesign, ResumeSummaryDto, ResumeVersionDto } from "../types/cv";

export function VariantsPage() {
  const { resumeId: resumeIdParam } = useParams();
  const navigate = useNavigate();

  const [summaries, setSummaries] = useState<ResumeSummaryDto[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [currentTitle, setCurrentTitle] = useState<string | null>(null);
  const [versions, setVersions] = useState<ResumeVersionDto[]>([]);
  const [selectedResumeId, setSelectedResumeId] = useState("");
  const [pdfDesign, setPdfDesign] = useState<PdfDesign>("A");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadArbeitsversion = useCallback(async (id: string) => {
    setBusy(true);
    setError(null);
    try {
      const arbeitsversion = await api.getResume(id);
      const v = await api.listVersions(id);
      const list = await api.listResumes();
      setSummaries(list);
      setVersions(v);
      setActiveId(id);
      setSelectedResumeId(id);
      setCurrentTitle(arbeitsversion.title);
      setLastResumeId(id);
      navigate(`/varianten/${id}`, { replace: true });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }, [navigate]);

  useEffect(() => {
    void (async () => {
      setBusy(true);
      setError(null);
      try {
        const list = await api.listResumes();
        setSummaries(list);
        const fromRoute = resumeIdParam?.trim();
        if (fromRoute) {
          await loadArbeitsversion(fromRoute);
        } else {
          const last = getLastResumeId();
          if (last) {
            await loadArbeitsversion(last);
          }
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : String(e));
      } finally {
        setBusy(false);
      }
    })();
  }, [resumeIdParam, loadArbeitsversion]);

  const loadSelected = async () => {
    if (!selectedResumeId) {
      setError("Bitte Arbeitsversion auswaehlen.");
      return;
    }
    await loadArbeitsversion(selectedResumeId);
  };

  const alsArbeitsversion = (variante: ResumeVersionDto) => {
    if (!activeId) {
      return;
    }
    const name = variante.label?.trim() ? variante.label : `v${variante.versionNumber}`;
    notify(`Variante ${name} wird als Arbeitsversion geladen.`);
    navigate(`/arbeitsversion/${activeId}?versionId=${variante.id}`);
  };

  const umbenennen = async (variante: ResumeVersionDto) => {
    if (!activeId) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await api.updateVersion(activeId, variante.id, {
        label: variante.label?.trim() ? variante.label.trim() : null,
      });
      await loadArbeitsversion(activeId);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const loeschen = async (variante: ResumeVersionDto) => {
    if (!activeId) {
      return;
    }
    if (!window.confirm(`Variante v${variante.versionNumber} loeschen?`)) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await api.deleteVersion(activeId, variante.id);
      await loadArbeitsversion(activeId);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const exportPdf = async (variante: ResumeVersionDto) => {
    if (!activeId) {
      return;
    }
    const blob = await api.downloadPdf(activeId, { versionId: variante.id, design: pdfDesign });
    const name = variante.label?.trim() ? variante.label.replace(/\s+/g, "-") : `v${variante.versionNumber}`;
    downloadBlob(`variante-${name}.pdf`, blob);
  };

  const exportDocx = async (variante: ResumeVersionDto) => {
    if (!activeId) {
      return;
    }
    const blob = await api.downloadDocx(activeId, variante.id);
    const name = variante.label?.trim() ? variante.label.replace(/\s+/g, "-") : `v${variante.versionNumber}`;
    downloadBlob(`variante-${name}.docx`, blob);
  };

  const goEditor = () => {
    if (!activeId) {
      navigate("/");
      return;
    }
    notify("Zurueck zur Arbeitsversion.");
    navigate(`/arbeitsversion/${activeId}`);
  };

  const updateVersionLabel = (id: string, label: string) => {
    setVersions((prev) => prev.map((x) => (x.id === id ? { ...x, label } : x)));
  };

  return (
    <section className="resume-shell">
      <header className="toolbar">
        <div className="toolbar-left">
          <h1>Gespeicherte Varianten</h1>
          <small>{currentTitle ?? "Arbeitsversion waehlen"}</small>
          {activeId ? <small>Arbeitsversion-ID: {activeId}</small> : null}
        </div>
        <div className="toolbar-actions">
          <label className="pdf-design-selector">
            PDF Design
            <select className="form-control" value={pdfDesign} onChange={(e) => setPdfDesign(e.target.value as PdfDesign)}>
              <option value="A">Design A — Klassisch (ATS)</option>
              <option value="B">Design B — Modern</option>
              <option value="C">Design C — Professional</option>
            </select>
          </label>
          <select
            className="form-control version-label"
            value={selectedResumeId}
            onChange={(e) => setSelectedResumeId(e.target.value)}
            disabled={busy}
          >
            <option value="">Arbeitsversion waehlen</option>
            {summaries.map((r) => (
              <option key={r.id} value={r.id}>
                {r.title} ({new Date(r.updatedAtUtc).toLocaleString()})
              </option>
            ))}
          </select>
          <button type="button" className="btn btn-secondary" onClick={() => void loadSelected()} disabled={busy}>
            <i className="bi bi-arrow-repeat" /> Laden
          </button>
          <button type="button" className="btn btn-secondary" onClick={() => goEditor()}>
            <i className="bi bi-arrow-return-left" /> Zur Arbeitsversion
          </button>
        </div>
      </header>

      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}

      <div className="context-banner" role="status">
        <i className="bi bi-pencil-square" />
        <strong>Variante = feste Kopie. Du kannst sie umbenennen, loeschen, exportieren oder als Arbeitsversion laden.</strong>
      </div>

      <section className="versions-panel">
        <h2>Variantenliste</h2>
        {busy ? <p className="muted">Lade...</p> : null}
        {!activeId ? <p className="muted">Bitte zuerst eine Arbeitsversion auswaehlen.</p> : null}
        {activeId && versions.length === 0 ? <p className="muted">Noch keine gespeicherten Varianten vorhanden.</p> : null}
        {activeId && versions.length > 0 ? (
          <table className="table table-sm variants-table">
            <thead>
              <tr>
                <th>Variante</th>
                <th>Name</th>
                <th>Erstellt</th>
                <th>Aktionen</th>
              </tr>
            </thead>
            <tbody>
              {versions.map((variante) => (
                <tr key={variante.id}>
                  <td>
                    <span className={`version-badge ${versionBadgeClass(variante.versionNumber)}`}>
                      v{variante.versionNumber}
                    </span>
                  </td>
                  <td style={{ minWidth: 280 }}>
                    <input
                      className="form-control"
                      value={variante.label ?? ""}
                      onChange={(e) => updateVersionLabel(variante.id, e.target.value)}
                    />
                  </td>
                  <td>{new Date(variante.createdAtUtc).toLocaleString()}</td>
                  <td>
                    <div className="variants-actions">
                      <button type="button" className="btn btn-sm btn-ghost" onClick={() => alsArbeitsversion(variante)}>
                        <i className="bi bi-pencil-square" /> Als Arbeitsversion
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-ghost"
                        onClick={() => void umbenennen(variante)}
                        disabled={busy}
                      >
                        <i className="bi bi-pencil" /> Umbenennen
                      </button>
                      <button type="button" className="btn btn-sm btn-export-pdf" onClick={() => void exportPdf(variante)}>
                        <i className="bi bi-download" /> PDF
                      </button>
                      <button type="button" className="btn btn-sm btn-export-docx" onClick={() => void exportDocx(variante)}>
                        <i className="bi bi-download" /> DOCX
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm btn-danger variant-delete-btn"
                        onClick={() => void loeschen(variante)}
                        disabled={busy}
                      >
                        <i className="bi bi-trash3" /> <span className="variant-delete-label">Loeschen</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </section>
    </section>
  );
}
