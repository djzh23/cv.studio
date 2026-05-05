import { useEffect, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import * as api from "../api/resumeApi";
import { AtsModal } from "../components/AtsModal";
import { LivePreview } from "../components/LivePreview";
import { NewResumeWizard } from "../components/NewResumeWizard";
import { useResumeEditor } from "../hooks/useResumeEditor";
import { downloadBlob, getLastResumeId, notify } from "../lib/cvStudio";
import { formatVariantenName, versionBadgeClass } from "../lib/formatting";
import type { PdfDesign, ResumeVersionDto, SkillGroupData, WorkItemData } from "../types/cv";

function splitLines(input: string | undefined): string[] {
  return (input ?? "")
    .split("\n")
    .map((s) => s.trim())
    .filter(Boolean);
}

function splitComma(input: string | undefined): string[] {
  return (input ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

type TabId = "profil" | "beruf" | "ausbildung" | "kenntnisse" | "hobby" | "cvTitel";

export function HomePage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const versionId = searchParams.get("versionId");
  const navigate = useNavigate();

  const vm = useResumeEditor();
  const {
    templates,
    summaries,
    resume,
    versions,
    selectedTemplateKey,
    setSelectedTemplateKey,
    pdfDesign,
    setPdfDesign,
    busy,
    error,
    variantNameDraft,
    setVariantNameDraft,
    aktivKontextText,
    autoSaveText,
    loadTemplates,
    createArbeitsversion,
    openResume,
    updateResume,
    flushAutoSave,
    saveVariant,
    loadVariantIntoEditor,
    resetAll,
  } = vm;

  const [activeTab, setActiveTab] = useState<TabId>("profil");
  const [showSaveDropdown, setShowSaveDropdown] = useState(false);
  const [atsOpen, setAtsOpen] = useState(false);
  const [wizardOpen, setWizardOpen] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const [exportBusy, setExportBusy] = useState(false);

  const designParam = searchParams.get("design");

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      await loadTemplates();
      if (cancelled) {
        return;
      }
      if (id) {
        await openResume(id);
        if (cancelled) {
          return;
        }
        if (versionId) {
          await loadVariantIntoEditor(versionId);
        }
        if (designParam === "B" || designParam === "C") {
          setPdfDesign(designParam as PdfDesign);
        }
      } else {
        const last = getLastResumeId();
        if (last) {
          navigate(`/arbeitsversion/${last}`, { replace: true });
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, versionId]);

  const handleWizardCreated = (resumeId: string, selectedDesign: PdfDesign) => {
    setWizardOpen(false);
    const designQ = selectedDesign !== "A" ? `?design=${selectedDesign}` : "";
    navigate(`/arbeitsversion/${resumeId}${designQ}`);
  };

  const oeffneArbeitsversion = (resumeId: string) => {
    navigate(`/arbeitsversion/${resumeId}`);
  };

  const reloadTemplates = async () => {
    await loadTemplates();
  };

  const varianteSpeichern = async () => {
    const variante = await saveVariant();
    if (variante) {
      notify(`Gespeicherte Variante erstellt: ${formatVariantenName(variante)}`);
    }
  };

  const confirmVariantBeforeExport = async (): Promise<boolean> => {
    const speichern = window.confirm("Moechtest du den aktuellen Stand vor dem Export als gespeicherte Variante sichern?");
    if (!speichern) {
      return true;
    }
    const vorgeschlagen = `Variante ${new Date().toISOString().slice(0, 16).replace("T", " ")}`;
    const name = window.prompt("Name fuer gespeicherte Variante:", vorgeschlagen);
    if (name === null) {
      return false;
    }
    const v = await saveVariant(name);
    return v !== null;
  };

  const runExport = async (fn: () => Promise<void>): Promise<void> => {
    setExportError(null);
    setExportBusy(true);
    try {
      await fn();
    } catch (err) {
      setExportError(err instanceof Error ? err.message : "Export fehlgeschlagen. Bitte erneut versuchen.");
    } finally {
      setExportBusy(false);
    }
  };

  const exportArbeitsversionPdf = () =>
    void runExport(async () => {
      if (!(await confirmVariantBeforeExport())) {
        return;
      }
      if (!resume) {
        return;
      }
      await flushAutoSave();
      const blob = await api.downloadPdf(resume.id, { design: pdfDesign });
      downloadBlob(`arbeitsversion-${resume.id}.pdf`, blob);
    });

  const exportArbeitsversionDocx = () =>
    void runExport(async () => {
      if (!(await confirmVariantBeforeExport())) {
        return;
      }
      if (!resume) {
        return;
      }
      await flushAutoSave();
      const blob = await api.downloadDocx(resume.id);
      downloadBlob(`arbeitsversion-${resume.id}.docx`, blob);
    });

  const exportVariantePdf = (variante: ResumeVersionDto) =>
    void runExport(async () => {
      if (!resume) {
        return;
      }
      const blob = await api.downloadPdf(resume.id, { versionId: variante.id, design: pdfDesign });
      const name = formatVariantenName(variante).replace(/\s+/g, "-");
      downloadBlob(`variante-${name}.pdf`, blob);
    });

  const exportVarianteDocx = (variante: ResumeVersionDto) =>
    void runExport(async () => {
      if (!resume) {
        return;
      }
      const blob = await api.downloadDocx(resume.id, variante.id);
      const name = formatVariantenName(variante).replace(/\s+/g, "-");
      downloadBlob(`variante-${name}.docx`, blob);
    });

  const varianteLaden = async (variantenId: string) => {
    await loadVariantIntoEditor(variantenId);
    notify("Variante als Arbeitsversion geladen.");
  };

  const saveWithoutExport = async () => {
    await varianteSpeichern();
    setShowSaveDropdown(false);
  };

  const saveAndExportPdf = () => {
    setShowSaveDropdown(false);
    void runExport(async () => {
      const variante = await saveVariant();
      if (!variante || !resume) {
        return;
      }
      const blob = await api.downloadPdf(resume.id, { versionId: variante.id, design: pdfDesign });
      const name = formatVariantenName(variante).replace(/\s+/g, "-");
      downloadBlob(`variante-${name}.pdf`, blob);
    });
  };

  const saveAndExportDocx = () => {
    setShowSaveDropdown(false);
    void runExport(async () => {
      const variante = await saveVariant();
      if (!variante || !resume) {
        return;
      }
      const blob = await api.downloadDocx(resume.id, variante.id);
      const name = formatVariantenName(variante).replace(/\s+/g, "-");
      downloadBlob(`variante-${name}.docx`, blob);
    });
  };

  const freshStart = async () => {
    if (!window.confirm("Alle Arbeitsversionen und gespeicherten Varianten loeschen?")) {
      return;
    }
    await resetAll();
    navigate("/", { replace: true });
  };

  const lastSnapshotForAts =
    versions.length === 0
      ? null
      : [...versions].sort((a, b) => b.versionNumber - a.versionNumber)[0]?.resumeData ?? null;

  const tabClass = (t: TabId) => (activeTab === t ? "active" : "");

  if (!resume) {
    return (
      <section className="empty-state">
        <h1>CvStudio</h1>
        <p>Erstelle deinen ersten Lebenslauf in wenigen Schritten.</p>

        {error ? (
          <div className="alert alert-danger" role="alert">
            {error}
          </div>
        ) : null}

        <div className="toolbar-actions" style={{ justifyContent: "center", marginBottom: "0.75rem" }}>
          <button type="button" className="btn btn-primary btn-lg" onClick={() => setWizardOpen(true)} disabled={busy}>
            <i className="bi bi-plus-circle" /> Neuer Lebenslauf
          </button>
          <button type="button" className="btn btn-danger" onClick={() => void freshStart()} disabled={busy}>
            <i className="bi bi-trash3" /> Fresh Start
          </button>
        </div>

        {summaries.length > 0 ? (
          <section className="versions-panel" style={{ marginTop: "1rem", textAlign: "left" }}>
            <h2>Bestehende Arbeitsversionen</h2>
            <div className="versions-list">
              {summaries.slice(0, 12).map((arbeitsversion) => (
                <button
                  key={arbeitsversion.id}
                  type="button"
                  className="version-item"
                  onClick={() => oeffneArbeitsversion(arbeitsversion.id)}
                >
                  <strong>{arbeitsversion.title}</strong>
                  <span>ID: {arbeitsversion.id}</span>
                  <small>Zuletzt geaendert: {new Date(arbeitsversion.updatedAtUtc).toLocaleString()}</small>
                </button>
              ))}
            </div>
          </section>
        ) : null}
        <NewResumeWizard
          isOpen={wizardOpen}
          onClose={() => setWizardOpen(false)}
          onCreated={handleWizardCreated}
          summaries={summaries}
        />
      </section>
    );
  }

  const d = resume.resumeData;
  const st = d.sectionTitles ?? {};
  const hasDemoData =
    d.profile.firstName === "Max" && d.profile.lastName === "Mustermann" ||
    d.profile.email === "max.mustermann@example.de" ||
    d.profile.location.includes("Musterstraße") ||
    d.profile.phone === "+49 (0) 170 000 00 00";

  return (
    <section className="resume-shell">
      <header className="toolbar">
        <div className="toolbar-left">
          <h1>
            {resume.title}
            {hasDemoData && (
              <span className="demo-data-badge" title="Dieser Lebenslauf enthält noch Platzhalter-Daten. Ersetze sie durch deine echten Daten.">
                Demo-Daten
              </span>
            )}
          </h1>
          <small className="header-meta-row">
            <span>
              <i className="bi bi-pencil-square" /> Du bearbeitest: {aktivKontextText}
            </span>
            <span className="meta-dot">&bull;</span>
            <span className="autosave-state">
              <span className="autosave-dot" />
              {autoSaveText}
            </span>
          </small>
          <small className="mono-meta">Arbeitsversion-ID: {resume.id}</small>
        </div>
        <div className="toolbar-actions">
          <button type="button" className="btn btn-primary" onClick={() => setWizardOpen(true)} disabled={busy}>
            <i className="bi bi-plus-circle" /> Neuer Lebenslauf
          </button>
          <button type="button" className="btn btn-secondary" onClick={() => navigate(`/varianten/${resume.id}`)}>
            <i className="bi bi-git" /> Varianten verwalten
          </button>
        </div>
      </header>

      <NewResumeWizard
        isOpen={wizardOpen}
        onClose={() => setWizardOpen(false)}
        onCreated={handleWizardCreated}
        summaries={summaries}
      />

      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}

      <div className="context-banner" role="status">
        <i className="bi bi-pencil-square" />
        <strong>Du bearbeitest: {aktivKontextText}</strong>
      </div>

      <main className="grid-area">
        <section className="editor-panel">
          <div className="editor-scroll">
            <div className="editor-toolbar">
              <div className="toolbar-actions-row">
                <div className="toolbar-left">
                  <span className="editing-label">
                    <i className="bi bi-pencil-square" /> Du bearbeitest: {aktivKontextText}
                  </span>
                </div>
                <div className="toolbar-right">
                  <div className="export-group">
                    <select
                      className="design-select"
                      value={pdfDesign}
                      onChange={(e) => setPdfDesign(e.target.value as PdfDesign)}
                    >
                      <option value="A">Design A — Klassisch (ATS)</option>
                      <option value="B">Design B — Modern</option>
                      <option value="C">Design C — Professional</option>
                    </select>
                    <button
                      type="button"
                      className="btn btn-export-pdf btn-md"
                      onClick={exportArbeitsversionPdf}
                      disabled={busy || exportBusy}
                    >
                      <i className="bi bi-download" /> PDF
                    </button>
                  </div>
                  <button
                    type="button"
                    className="btn btn-export-docx btn-md"
                    onClick={exportArbeitsversionDocx}
                    disabled={busy || exportBusy}
                  >

                    <i className="bi bi-download" /> DOCX
                  </button>
                  <button type="button" className="btn btn-secondary btn-md" onClick={() => setAtsOpen(true)} disabled={busy}>
                    <i className="bi bi-bullseye" /> ATS prüfen
                  </button>
                  <div className="save-dropdown-container">
                    <button
                      type="button"
                      className="btn btn-primary btn-md"
                      onClick={() => setShowSaveDropdown((s) => !s)}
                      disabled={busy}
                    >
                      <i className="bi bi-floppy" /> Variante speichern
                    </button>
                    {showSaveDropdown ? (
                      <div className="save-dropdown">
                        <div className="save-dropdown-title">Variante speichern</div>
                        <input
                          className="form-control"
                          placeholder="z.B. SAP Bewerbung"
                          value={variantNameDraft}
                          onChange={(e) => setVariantNameDraft(e.target.value)}
                        />
                        <div className="save-dropdown-actions">
                          <button type="button" className="btn btn-secondary" onClick={() => void saveWithoutExport()} disabled={busy}>
                            Speichern ohne Export
                          </button>
                          <button type="button" className="btn btn-primary" onClick={saveAndExportPdf} disabled={busy || exportBusy}>
                            Speichern + PDF exportieren
                          </button>
                          <button type="button" className="btn btn-primary" onClick={saveAndExportDocx} disabled={busy || exportBusy}>
                            Speichern + DOCX exportieren
                          </button>
                        </div>
                      </div>
                    ) : null}
                  </div>
                </div>
              </div>

              {exportError ? (
                <div className="export-error-bar" role="alert">
                  <i className="bi bi-exclamation-triangle-fill" />
                  <span className="export-error-message">{exportError}</span>
                  <button type="button" className="export-error-dismiss" onClick={() => setExportError(null)} aria-label="Schließen">
                    <i className="bi bi-x-lg" />
                  </button>
                </div>
              ) : null}

              <div className="toolbar-tabs-row">
                <button type="button" className={`tab-btn ${tabClass("profil")}`} onClick={() => setActiveTab("profil")}>
                  <i className="bi bi-person" /> Profil
                </button>
                <button type="button" className={`tab-btn ${tabClass("beruf")}`} onClick={() => setActiveTab("beruf")}>
                  <i className="bi bi-building" /> Beruf
                </button>
                <button type="button" className={`tab-btn ${tabClass("ausbildung")}`} onClick={() => setActiveTab("ausbildung")}>
                  <i className="bi bi-mortarboard" /> Ausbildung
                </button>
                <button type="button" className={`tab-btn ${tabClass("kenntnisse")}`} onClick={() => setActiveTab("kenntnisse")}>
                  <i className="bi bi-tools" /> Kenntnisse
                </button>
                <button type="button" className={`tab-btn ${tabClass("hobby")}`} onClick={() => setActiveTab("hobby")}>
                  <i className="bi bi-heart" /> Hobbys
                </button>
                <button type="button" className={`tab-btn ${tabClass("cvTitel")}`} onClick={() => setActiveTab("cvTitel")}>
                  <i className="bi bi-globe2" /> CV-Titel
                </button>
              </div>
            </div>

            {activeTab === "profil" ? (
              <div className="tab-content">
                <h2>Profil</h2>
                <div className="field-grid">
                  <label>
                    Titel
                    <input
                      className="form-control"
                      value={resume.title}
                      onChange={(e) => updateResume((r) => void (r.title = e.target.value))}
                    />
                  </label>
                  <label>
                    Vorname
                    <input
                      className="form-control"
                      value={d.profile.firstName}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.firstName = e.target.value))}
                    />
                  </label>
                  <label>
                    Nachname
                    <input
                      className="form-control"
                      value={d.profile.lastName}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.lastName = e.target.value))}
                    />
                  </label>
                  <label>
                    Headline
                    <input
                      className="form-control"
                      value={d.profile.headline}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.headline = e.target.value))}
                    />
                  </label>
                  <label>
                    E-Mail
                    <input
                      className="form-control"
                      value={d.profile.email}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.email = e.target.value))}
                    />
                  </label>
                  <label>
                    Telefon
                    <input
                      className="form-control"
                      value={d.profile.phone}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.phone = e.target.value))}
                    />
                  </label>
                  <label>
                    Ort
                    <input
                      className="form-control"
                      value={d.profile.location}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.location = e.target.value))}
                    />
                  </label>
                  <label>
                    Profilbild URL
                    <input
                      className="form-control"
                      value={d.profile.profileImageUrl}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.profileImageUrl = e.target.value))}
                    />
                  </label>
                </div>
                <div className="form-section-divider">
                  <span>
                    Online-Profile <small>(optional)</small>
                  </span>
                </div>
                <div className="field-grid">
                  <label>
                    LinkedIn-URL
                    <input
                      className="form-control"
                      value={d.profile.linkedInUrl ?? ""}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.linkedInUrl = e.target.value))}
                    />
                    <small className="form-hint">Wird im PDF nur angezeigt wenn ausgefuellt.</small>
                  </label>
                  <label>
                    GitHub-URL
                    <input
                      className="form-control"
                      value={d.profile.gitHubUrl ?? ""}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.gitHubUrl = e.target.value))}
                    />
                  </label>
                  <label>
                    Portfolio / Website
                    <input
                      className="form-control"
                      value={d.profile.portfolioUrl ?? ""}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.portfolioUrl = e.target.value))}
                    />
                  </label>
                  <label>
                    Arbeitsgenehmigung
                    <input
                      className="form-control"
                      type="text"
                      list="work-permit-options"
                      placeholder="Arbeitsgenehmigung eingeben oder auswaehlen..."
                      value={d.profile.workPermit ?? ""}
                      onChange={(e) => updateResume((r) => void (r.resumeData.profile.workPermit = e.target.value))}
                    />
                    <small className="form-hint">Erscheint als diskreter Hinweis im PDF-Header.</small>
                  </label>
                </div>
                <datalist id="work-permit-options">
                  <option value="EU-Staatsbuerger · Unbeschraenkte Arbeitserlaubnis Deutschland" />
                  <option value="Aufenthaltstitel § 18b AufenthG · Unbeschraenkte Arbeitserlaubnis" />
                  <option value="Niederlassungserlaubnis · Dauerhaft arbeitsberechtigt in Deutschland" />
                  <option value="EU Blue Card · Hochqualifiziert arbeitsberechtigt in Deutschland" />
                </datalist>
                <label>
                  Qualifikationsprofil
                  <textarea
                    className="form-control"
                    rows={6}
                    value={d.profile.summary}
                    onChange={(e) => updateResume((r) => void (r.resumeData.profile.summary = e.target.value))}
                  />
                </label>
              </div>
            ) : null}

            {activeTab === "beruf" ? (
              <div className="tab-content">
                <h2>Berufserfahrung</h2>
                {d.workItems.map((item, index) => (
                  <div key={index} className="list-card">
                    <div className="field-grid">
                      <label>
                        Unternehmen
                        <input
                          className="form-control"
                          value={item.company}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.workItems[index].company = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Rolle
                        <input
                          className="form-control"
                          value={item.role}
                          onChange={(e) => updateResume((r) => void (r.resumeData.workItems[index].role = e.target.value))}
                        />
                      </label>
                      <label>
                        Start
                        <input
                          className="form-control"
                          value={item.startDate}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.workItems[index].startDate = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Ende
                        <input
                          className="form-control"
                          value={item.endDate}
                          onChange={(e) => updateResume((r) => void (r.resumeData.workItems[index].endDate = e.target.value))}
                        />
                      </label>
                    </div>
                    <label>
                      Kurzbeschreibung
                      <textarea
                        className="form-control"
                        rows={3}
                        value={item.description}
                        onChange={(e) =>
                          updateResume((r) => void (r.resumeData.workItems[index].description = e.target.value))
                        }
                      />
                    </label>
                    <label>
                      Stichpunkte (eine Zeile = ein Punkt)
                      <textarea
                        className="form-control"
                        rows={4}
                        value={item.bullets.join("\n")}
                        onChange={(e) =>
                          updateResume((r) => void (r.resumeData.workItems[index].bullets = splitLines(e.target.value)))
                        }
                      />
                    </label>
                    <button
                      type="button"
                      className="btn btn-sm btn-danger"
                      onClick={() =>
                        updateResume((r) => {
                          r.resumeData.workItems.splice(index, 1);
                        })
                      }
                    >
                      <i className="bi bi-trash3" /> Entfernen
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  className="btn btn-sm btn-secondary"
                  onClick={() =>
                    updateResume((r) => {
                      r.resumeData.workItems.push({
                        company: "",
                        role: "",
                        startDate: "",
                        endDate: "",
                        description: "",
                        bullets: [],
                      } satisfies WorkItemData);
                    })
                  }
                >
                  <i className="bi bi-plus-circle" /> Eintrag hinzufuegen
                </button>
              </div>
            ) : null}

            {activeTab === "ausbildung" ? (
              <div className="tab-content">
                <h2>Ausbildung</h2>
                {d.educationItems.map((item, index) => (
                  <div key={index} className="list-card">
                    <div className="field-grid">
                      <label>
                        Schule
                        <input
                          className="form-control"
                          value={item.school}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.educationItems[index].school = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Abschluss
                        <input
                          className="form-control"
                          value={item.degree}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.educationItems[index].degree = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Start
                        <input
                          className="form-control"
                          value={item.startDate}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.educationItems[index].startDate = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Ende
                        <input
                          className="form-control"
                          value={item.endDate}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.educationItems[index].endDate = e.target.value))
                          }
                        />
                      </label>
                    </div>
                    <button
                      type="button"
                      className="btn btn-sm btn-danger"
                      onClick={() =>
                        updateResume((r) => {
                          r.resumeData.educationItems.splice(index, 1);
                        })
                      }
                    >
                      <i className="bi bi-trash3" /> Entfernen
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  className="btn btn-sm btn-secondary"
                  onClick={() =>
                    updateResume((r) => {
                      r.resumeData.educationItems.push({
                        school: "",
                        degree: "",
                        startDate: "",
                        endDate: "",
                      });
                    })
                  }
                >
                  <i className="bi bi-plus-circle" /> Eintrag hinzufuegen
                </button>
              </div>
            ) : null}

            {activeTab === "kenntnisse" ? (
              <div className="tab-content">
                <h2>Kenntnisse</h2>
                {d.skills.map((group, index) => (
                  <div key={index} className="list-card">
                    <div className="field-grid">
                      <label>
                        Kategorie
                        <input
                          className="form-control"
                          value={group.categoryName}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.skills[index].categoryName = e.target.value))
                          }
                        />
                      </label>
                      <label>
                        Eintraege (kommagetrennt)
                        <input
                          className="form-control"
                          value={group.items.join(", ")}
                          onChange={(e) =>
                            updateResume((r) => void (r.resumeData.skills[index].items = splitComma(e.target.value)))
                          }
                        />
                      </label>
                    </div>
                    <button
                      type="button"
                      className="btn btn-sm btn-danger"
                      onClick={() =>
                        updateResume((r) => {
                          r.resumeData.skills.splice(index, 1);
                        })
                      }
                    >
                      <i className="bi bi-trash3" /> Entfernen
                    </button>
                  </div>
                ))}
                <button
                  type="button"
                  className="btn btn-sm btn-secondary"
                  onClick={() =>
                    updateResume((r) => {
                      r.resumeData.skills.push({ categoryName: "", items: [] } satisfies SkillGroupData);
                    })
                  }
                >
                  <i className="bi bi-plus-circle" /> Kategorie hinzufuegen
                </button>
              </div>
            ) : null}

            {activeTab === "cvTitel" ? (
              <div className="tab-content">
                <h2>CV-Sektionstitel</h2>
                <p className="muted">
                  Optional: Ueberschriften fuer PDF, DOCX und Live-Vorschau (z.B. Franzoesisch). Leer lassen = deutscher
                  Standard.
                </p>
                <div className="field-grid">
                  <label>
                    Qualifikationsprofil
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.qualificationsProfile ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.qualificationsProfile = e.target.value;
                        })
                      }
                      placeholder="Profil de qualifications"
                    />
                  </label>
                  <label>
                    Berufserfahrung
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.workExperience ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.workExperience = e.target.value;
                        })
                      }
                      placeholder="Expérience professionnelle"
                    />
                  </label>
                  <label>
                    Ausbildung
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.education ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.education = e.target.value;
                        })
                      }
                      placeholder="Formation"
                    />
                  </label>
                  <label>
                    Kenntnisse
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.skills ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.skills = e.target.value;
                        })
                      }
                      placeholder="Compétences"
                    />
                  </label>
                  <label>
                    Projekte
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.projects ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.projects = e.target.value;
                        })
                      }
                      placeholder="Projets"
                    />
                  </label>
                  <label>
                    Sprachen &amp; Interessen (Block)
                    <input
                      className="form-control"
                      maxLength={140}
                      value={st.languagesAndInterests ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.languagesAndInterests = e.target.value;
                        })
                      }
                      placeholder="Langues et centres d'intérêt"
                    />
                  </label>
                  <label>
                    Kontakte (PDF C)
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.contacts ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.contacts = e.target.value;
                        })
                      }
                      placeholder="Coordonnées"
                    />
                  </label>
                  <label>
                    Sprachen (PDF C)
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.languages ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.languages = e.target.value;
                        })
                      }
                      placeholder="Langues"
                    />
                  </label>
                  <label>
                    Interessen / Hobbys (PDF C)
                    <input
                      className="form-control"
                      maxLength={120}
                      value={st.interests ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.interests = e.target.value;
                        })
                      }
                      placeholder="Centres d'intérêt"
                    />
                  </label>
                </div>
                <div className="form-section-divider">
                  <span>Zeilen-Labels (PDF A/B, DOCX)</span>
                </div>
                <div className="field-grid">
                  <label>
                    Prefix Sprachenliste
                    <input
                      className="form-control"
                      maxLength={80}
                      value={st.languagesInlineLabel ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.languagesInlineLabel = e.target.value;
                        })
                      }
                      placeholder="Langues : "
                    />
                  </label>
                  <label>
                    Prefix Hobbys
                    <input
                      className="form-control"
                      maxLength={80}
                      value={st.interestsInlineLabel ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.interestsInlineLabel = e.target.value;
                        })
                      }
                      placeholder="Centres d'intérêt : "
                    />
                  </label>
                  <label>
                    PDF Modern: Zeile Sprachen
                    <input
                      className="form-control"
                      maxLength={80}
                      value={st.designBLanguagesRowLabel ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.designBLanguagesRowLabel = e.target.value;
                        })
                      }
                      placeholder="Langues : "
                    />
                  </label>
                  <label>
                    PDF Modern: Zeile Hobbys
                    <input
                      className="form-control"
                      maxLength={80}
                      value={st.designBInterestsRowLabel ?? ""}
                      onChange={(e) =>
                        updateResume((r) => {
                          if (!r.resumeData.sectionTitles) {
                            r.resumeData.sectionTitles = {};
                          }
                          r.resumeData.sectionTitles.designBInterestsRowLabel = e.target.value;
                        })
                      }
                      placeholder="Centres d'intérêt : "
                    />
                  </label>
                </div>
              </div>
            ) : null}

            {activeTab === "hobby" ? (
              <div className="tab-content">
                <h2>Hobbys</h2>
                <label>
                  Hobbys (eine Zeile = ein Hobby)
                  <textarea
                    className="form-control"
                    rows={8}
                    value={d.hobbies.join("\n")}
                    onChange={(e) =>
                      updateResume((r) => void (r.resumeData.hobbies = splitLines(e.target.value)))
                    }
                  />
                </label>
              </div>
            ) : null}
          </div>
        </section>

        <section className="preview-panel">
          <h2 className="preview-label">
            <i className="bi bi-eye" /> Live-Vorschau
          </h2>
          <div className="preview-scroll">
            <LivePreview resume={resume} pdfDesign={pdfDesign} />
          </div>
        </section>
      </main>

      <section className="versions-panel">
        <h2>
          <i className="bi bi-layers" /> Gespeicherte Varianten
        </h2>
        <p className="muted">2. Schritt: Variante speichern. 3. Schritt: Direkt aus Variante exportieren.</p>
        {versions.length === 0 ? (
          <p className="muted">Noch keine gespeicherte Variante vorhanden.</p>
        ) : (
          <div className="versions-list">
            {versions.map((variante) => (
              <div key={variante.id} className="version-item">
                <strong>
                  <span className={`version-badge ${versionBadgeClass(variante.versionNumber)}`}>
                    v{variante.versionNumber}
                  </span>{" "}
                  {formatVariantenName(variante)}
                </strong>
                <small>{new Date(variante.createdAtUtc).toLocaleString()}</small>
                <div className="toolbar-actions">
                  <button type="button" className="btn btn-sm btn-ghost" onClick={() => void varianteLaden(variante.id)}>
                    <i className="bi bi-pencil-square" /> Als Arbeitsversion laden
                  </button>
                  <button type="button" className="btn btn-sm btn-export-pdf" onClick={() => exportVariantePdf(variante)} disabled={exportBusy}>
                    <i className="bi bi-download" /> PDF
                  </button>
                  <button type="button" className="btn btn-sm btn-export-docx" onClick={() => exportVarianteDocx(variante)} disabled={exportBusy}>
                    <i className="bi bi-download" /> DOCX
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <AtsModal
        isOpen={atsOpen}
        onClose={() => setAtsOpen(false)}
        onExportAnyway={() => void exportArbeitsversionPdf()}
        currentResume={resume.resumeData}
        lastSnapshotResume={lastSnapshotForAts}
      />
    </section>
  );
}
