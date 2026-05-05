import { useEffect, useRef, useState } from "react";
import * as api from "../api/resumeApi";
import type { PdfDesign, ResumeSummaryDto } from "../types/cv";

type Purpose = "bewerbung" | "allgemein" | "ableiten";
type Step = 1 | 2 | 3;

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (resumeId: string, design: PdfDesign) => void;
  summaries: ResumeSummaryDto[];
}

const TEMPLATE_KEY = "software-developer";

const PURPOSE_OPTIONS: { id: Purpose; icon: string; title: string; desc: string }[] = [
  { id: "bewerbung", icon: "bi bi-briefcase", title: "Für eine Bewerbung", desc: "Wird mit einer Stelle verknüpft" },
  { id: "allgemein", icon: "bi bi-file-earmark-text", title: "Allgemeiner Lebenslauf", desc: "Ohne Stellenbezug" },
  { id: "ableiten", icon: "bi bi-copy", title: "Aus bestehendem CV ableiten", desc: "Kopiert einen vorhandenen CV" },
];

const DESIGN_OPTIONS: { id: PdfDesign; title: string; subtitle: string; badge?: string }[] = [
  { id: "A", title: "Klassisch", subtitle: "Einspaltiges Layout", badge: "ATS-optimiert" },
  { id: "B", title: "Modern", subtitle: "Zweispaltiges Layout" },
  { id: "C", title: "Professional", subtitle: "Sidebar-Layout" },
];

export function NewResumeWizard({ isOpen, onClose, onCreated, summaries }: Props) {
  const [step, setStep] = useState<Step>(1);
  const [purpose, setPurpose] = useState<Purpose | null>(null);
  const [design, setDesign] = useState<PdfDesign>("A");
  const [sourceResumeId, setSourceResumeId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const overlayRef = useRef<HTMLDivElement>(null);

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setStep(1);
      setPurpose(null);
      setDesign("A");
      setSourceResumeId(null);
      setBusy(false);
      setError(null);
    }
  }, [isOpen]);

  // Close on Escape
  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onClose();
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [isOpen, onClose]);

  if (!isOpen) {
    return null;
  }

  const canProceedStep1 = purpose !== null && (purpose !== "ableiten" || summaries.length > 0);
  const canProceedStep2 =
    purpose === "ableiten" ? sourceResumeId !== null : true;

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === overlayRef.current) {
      onClose();
    }
  };

  const goNext = () => {
    if (step === 1 && canProceedStep1) {
      setStep(2);
    } else if (step === 2 && canProceedStep2) {
      setStep(3);
    }
  };

  const goBack = () => {
    if (step === 2) {
      setStep(1);
    } else if (step === 3) {
      setStep(2);
    }
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      let resumeId: string;

      if (purpose === "ableiten" && sourceResumeId) {
        const source = await api.getResume(sourceResumeId);
        const sourceTitle = source.title;
        const created = await api.createResume({
          title: `Kopie — ${sourceTitle}`,
          templateKey: source.templateKey ?? TEMPLATE_KEY,
          resumeData: source.resumeData,
        });
        resumeId = created.id;
      } else {
        const created = await api.createResumeFromTemplate(TEMPLATE_KEY);
        resumeId = created.id;
      }

      onCreated(resumeId, design);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unbekannter Fehler beim Erstellen.");
    } finally {
      setBusy(false);
    }
  };

  const purposeLabel = PURPOSE_OPTIONS.find((p) => p.id === purpose)?.title ?? "";
  const designLabel = DESIGN_OPTIONS.find((d) => d.id === design)?.title ?? "";
  const sourceLabel = summaries.find((s) => s.id === sourceResumeId)?.title ?? "";

  return (
    <div className="wizard-overlay" ref={overlayRef} onClick={handleOverlayClick} role="dialog" aria-modal="true">
      <div className="wizard-modal">
        <div className="wizard-header">
          <div className="wizard-progress">
            {([1, 2, 3] as Step[]).map((s) => (
              <div key={s} className={`wizard-dot ${step >= s ? "wizard-dot-active" : ""}`} />
            ))}
          </div>
          <button type="button" className="wizard-close" onClick={onClose} aria-label="Schließen">
            <i className="bi bi-x-lg" />
          </button>
        </div>

        <div className="wizard-body">
          {/* Step 1 — Zweck */}
          {step === 1 && (
            <div className="wizard-step">
              <h2 className="wizard-title">Neuer Lebenslauf</h2>
              <p className="wizard-subtitle">Wofür brauchst du ihn?</p>
              <div className="wizard-cards">
                {PURPOSE_OPTIONS.map((opt) => {
                  const disabled = opt.id === "ableiten" && summaries.length === 0;
                  return (
                    <button
                      key={opt.id}
                      type="button"
                      className={`wizard-card ${purpose === opt.id ? "wizard-card-active" : ""} ${disabled ? "wizard-card-disabled" : ""}`}
                      onClick={() => !disabled && setPurpose(opt.id)}
                      disabled={disabled}
                    >
                      {purpose === opt.id && (
                        <i className="bi bi-check-circle-fill wizard-card-check" />
                      )}
                      <i className={`${opt.icon} wizard-card-icon`} />
                      <strong>{opt.title}</strong>
                      <span>
                        {opt.id === "ableiten" && summaries.length === 0
                          ? "Noch keine CVs vorhanden"
                          : opt.desc}
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Step 2 — Design oder Quell-CV */}
          {step === 2 && purpose !== "ableiten" && (
            <div className="wizard-step">
              <h2 className="wizard-title">Wähle ein Design</h2>
              <p className="wizard-subtitle">
                Das Layout deines Lebenslaufs — Inhalte kannst du danach frei anpassen.
              </p>
              <div className="wizard-cards">
                {DESIGN_OPTIONS.map((opt) => (
                  <button
                    key={opt.id}
                    type="button"
                    className={`wizard-card wizard-card-design ${design === opt.id ? "wizard-card-active" : ""}`}
                    onClick={() => setDesign(opt.id)}
                  >
                    {design === opt.id && (
                      <i className="bi bi-check-circle-fill wizard-card-check" />
                    )}
                    <div className="wizard-design-preview">
                      <DesignPreview design={opt.id} />
                    </div>
                    <div className="wizard-design-info">
                      <strong>
                        {opt.title}
                        {opt.badge ? (
                          <span className="wizard-ats-badge">{opt.badge}</span>
                        ) : null}
                      </strong>
                      <span>{opt.subtitle}</span>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 2 — Quell-CV wählen (Ableiten) */}
          {step === 2 && purpose === "ableiten" && (
            <div className="wizard-step">
              <h2 className="wizard-title">Welchen CV kopieren?</h2>
              <p className="wizard-subtitle">Wähle den Lebenslauf, von dem du ableiten möchtest.</p>
              <div className="wizard-source-list">
                {summaries.map((s) => (
                  <button
                    key={s.id}
                    type="button"
                    className={`wizard-source-item ${sourceResumeId === s.id ? "wizard-card-active" : ""}`}
                    onClick={() => setSourceResumeId(s.id)}
                  >
                    {sourceResumeId === s.id && (
                      <i className="bi bi-check-circle-fill wizard-card-check" />
                    )}
                    <i className="bi bi-file-earmark-text wizard-source-icon" />
                    <div>
                      <strong>{s.title}</strong>
                      <small>Zuletzt geändert: {new Date(s.updatedAtUtc).toLocaleDateString()}</small>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Step 3 — Bestätigung */}
          {step === 3 && (
            <div className="wizard-step">
              <h2 className="wizard-title">Alles bereit</h2>
              <p className="wizard-subtitle">Überprüfe deine Auswahl und erstelle den Lebenslauf.</p>
              <div className="wizard-summary">
                <div className="wizard-summary-row">
                  <span className="wizard-summary-label">Zweck</span>
                  <span className="wizard-summary-value">{purposeLabel}</span>
                </div>
                {purpose !== "ableiten" ? (
                  <div className="wizard-summary-row">
                    <span className="wizard-summary-label">Design</span>
                    <span className="wizard-summary-value">{designLabel}</span>
                  </div>
                ) : (
                  <div className="wizard-summary-row">
                    <span className="wizard-summary-label">Quelle</span>
                    <span className="wizard-summary-value">{sourceLabel}</span>
                  </div>
                )}
              </div>
              <p className="wizard-hint">
                <i className="bi bi-info-circle" /> Du kannst Design und Inhalte jederzeit im Editor ändern.
              </p>
              {error && (
                <div className="wizard-error">
                  <i className="bi bi-exclamation-triangle" /> {error}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="wizard-footer">
          {step > 1 ? (
            <button type="button" className="btn btn-secondary" onClick={goBack} disabled={busy}>
              <i className="bi bi-arrow-left" /> Zurück
            </button>
          ) : (
            <div />
          )}

          {step < 3 ? (
            <button
              type="button"
              className="btn btn-primary"
              onClick={goNext}
              disabled={step === 1 ? !canProceedStep1 : !canProceedStep2}
            >
              Weiter <i className="bi bi-arrow-right" />
            </button>
          ) : (
            <button
              type="button"
              className="btn btn-primary"
              onClick={() => void handleCreate()}
              disabled={busy}
            >
              {busy ? (
                <>
                  <span className="wizard-spinner" /> Wird erstellt…
                </>
              ) : (
                <>
                  CV erstellen <i className="bi bi-check2" />
                </>
              )}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function DesignPreview({ design }: { design: PdfDesign }) {
  if (design === "A") {
    return (
      <svg viewBox="0 0 80 110" className="wizard-svg-preview" aria-hidden="true">
        <rect x="4" y="4" width="72" height="102" rx="2" fill="#f8f8f8" stroke="#e0e0e0" strokeWidth="1" />
        <rect x="10" y="10" width="24" height="24" rx="12" fill="#dbeafe" />
        <rect x="38" y="12" width="30" height="5" rx="2" fill="#1a3a5c" />
        <rect x="38" y="20" width="20" height="3" rx="1" fill="#7d8a99" />
        <rect x="38" y="26" width="25" height="2" rx="1" fill="#c5d5e8" />
        <rect x="10" y="40" width="60" height="1" fill="#c5d5e8" />
        <rect x="10" y="46" width="60" height="3" rx="1" fill="#f2f6fa" />
        <rect x="14" y="47" width="30" height="1.5" rx="0.5" fill="#1a3a5c" />
        <rect x="14" y="52" width="50" height="2" rx="1" fill="#e0e0e0" />
        <rect x="14" y="56" width="40" height="2" rx="1" fill="#e0e0e0" />
        <rect x="10" y="64" width="60" height="3" rx="1" fill="#f2f6fa" />
        <rect x="14" y="65" width="25" height="1.5" rx="0.5" fill="#1a3a5c" />
        <rect x="14" y="70" width="50" height="2" rx="1" fill="#e0e0e0" />
        <rect x="14" y="74" width="45" height="2" rx="1" fill="#e0e0e0" />
        <rect x="14" y="78" width="35" height="2" rx="1" fill="#e0e0e0" />
        <rect x="10" y="86" width="60" height="3" rx="1" fill="#f2f6fa" />
        <rect x="14" y="87" width="20" height="1.5" rx="0.5" fill="#1a3a5c" />
        <rect x="14" y="92" width="45" height="2" rx="1" fill="#e0e0e0" />
      </svg>
    );
  }
  if (design === "B") {
    return (
      <svg viewBox="0 0 80 110" className="wizard-svg-preview" aria-hidden="true">
        <rect x="4" y="4" width="72" height="102" rx="2" fill="#f8f8f8" stroke="#e0e0e0" strokeWidth="1" />
        <rect x="10" y="10" width="20" height="20" rx="2" fill="#f1f2f6" stroke="#d5d7dc" strokeWidth="0.5" />
        <rect x="34" y="12" width="32" height="6" rx="2" fill="#222" />
        <rect x="34" y="21" width="22" height="3" rx="1" fill="#5f6c7a" />
        <rect x="10" y="34" width="30" height="2" rx="1" fill="#6b7280" />
        <rect x="10" y="42" width="60" height="2.5" rx="1" fill="#2c2f34" />
        <rect x="10" y="47" width="60" height="1" fill="#2c2f34" />
        <rect x="10" y="50" width="50" height="2" rx="1" fill="#7d8a99" />
        <rect x="10" y="54" width="55" height="2" rx="1" fill="#3e4652" />
        <rect x="14" y="58" width="45" height="1.5" rx="0.5" fill="#ddd" />
        <rect x="14" y="61" width="40" height="1.5" rx="0.5" fill="#ddd" />
        <rect x="10" y="68" width="60" height="2.5" rx="1" fill="#2c2f34" />
        <rect x="10" y="73" width="60" height="1" fill="#2c2f34" />
        <rect x="10" y="76" width="50" height="2" rx="1" fill="#7d8a99" />
        <rect x="14" y="80" width="42" height="1.5" rx="0.5" fill="#ddd" />
        <rect x="10" y="88" width="60" height="2.5" rx="1" fill="#2c2f34" />
        <rect x="10" y="93" width="60" height="1" fill="#2c2f34" />
        <rect x="10" y="96" width="45" height="2" rx="1" fill="#ddd" />
      </svg>
    );
  }
  // Design C — Sidebar
  return (
    <svg viewBox="0 0 80 110" className="wizard-svg-preview" aria-hidden="true">
      <rect x="4" y="4" width="72" height="102" rx="2" fill="#f8f8f8" stroke="#e0e0e0" strokeWidth="1" />
      <rect x="4" y="4" width="26" height="102" rx="2" fill="#374151" />
      <circle cx="17" cy="20" r="9" fill="#dbeafe" />
      <rect x="7" y="34" width="20" height="2" rx="1" fill="#6b7280" />
      <rect x="7" y="38" width="18" height="1.5" rx="0.5" fill="#9ca3af" />
      <rect x="7" y="41" width="16" height="1.5" rx="0.5" fill="#9ca3af" />
      <rect x="7" y="50" width="20" height="2" rx="1" fill="#6b7280" />
      <rect x="7" y="54" width="18" height="1.5" rx="0.5" fill="#9ca3af" />
      <rect x="7" y="57" width="14" height="1.5" rx="0.5" fill="#9ca3af" />
      <rect x="34" y="10" width="36" height="6" rx="1" fill="#0f1c2e" />
      <rect x="34" y="18" width="24" height="3" rx="1" fill="#06b6d4" />
      <rect x="34" y="24" width="28" height="1.5" rx="0.5" fill="#4b5563" />
      <rect x="34" y="32" width="36" height="2" rx="1" fill="#0f1c2e" />
      <rect x="34" y="35" width="36" height="0.5" fill="#e5e7eb" />
      <rect x="34" y="38" width="30" height="1.5" rx="0.5" fill="#4b5563" />
      <rect x="34" y="41" width="25" height="1.5" rx="0.5" fill="#4b5563" />
      <rect x="34" y="48" width="36" height="2" rx="1" fill="#0f1c2e" />
      <rect x="34" y="51" width="36" height="0.5" fill="#e5e7eb" />
      <rect x="34" y="54" width="32" height="1.5" rx="0.5" fill="#4b5563" />
      <rect x="38" y="57" width="28" height="1.5" rx="0.5" fill="#c0c0c0" />
      <rect x="38" y="60" width="25" height="1.5" rx="0.5" fill="#c0c0c0" />
      <rect x="34" y="67" width="36" height="2" rx="1" fill="#0f1c2e" />
      <rect x="34" y="70" width="36" height="0.5" fill="#e5e7eb" />
      <rect x="38" y="73" width="28" height="1.5" rx="0.5" fill="#c0c0c0" />
      <rect x="38" y="76" width="22" height="1.5" rx="0.5" fill="#c0c0c0" />
    </svg>
  );
}
