import { useCallback, useEffect, useMemo, useState } from "react";
import { analyzeAts } from "../api/resumeApi";
import type { JobCategory, ResumeData, AtsScoreResult } from "../types/cv";

type Props = {
  isOpen: boolean;
  onClose: () => void;
  onExportAnyway: () => void | Promise<void>;
  currentResume: ResumeData | null;
  lastSnapshotResume: ResumeData | null;
};

function resolveResume(
  current: ResumeData | null,
  last: ResumeData | null,
): { data: ResumeData | null; hint: string } {
  if (current) {
    return { data: current, hint: "● Aktuelle Arbeitsversion" };
  }
  if (last) {
    return { data: last, hint: "● Letzte gespeicherte Variante" };
  }
  return { data: null, hint: "● Keine CV-Daten geladen" };
}

function priorityClass(priority: string): string {
  if (priority === "Hoch") {
    return "priority-high";
  }
  if (priority === "Mittel") {
    return "priority-medium";
  }
  return "priority-low";
}

export function AtsModal({ isOpen, onClose, onExportAnyway, currentResume, lastSnapshotResume }: Props) {
  const [jobDescription, setJobDescription] = useState("");
  const [category, setCategory] = useState<JobCategory>(0);
  const [result, setResult] = useState<AtsScoreResult | null>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analyzeError, setAnalyzeError] = useState("");

  const { data: resumeToAnalyze, hint: sourceHint } = useMemo(
    () => resolveResume(currentResume, lastSnapshotResume),
    [currentResume, lastSnapshotResume],
  );

  const canAnalyze = jobDescription.trim().length >= 100 && resumeToAnalyze !== null;

  const handleClose = useCallback(() => {
    setResult(null);
    setJobDescription("");
    setAnalyzeError("");
    setIsAnalyzing(false);
    setCategory(0);
    onClose();
  }, [onClose]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        handleClose();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [isOpen, handleClose]);

  if (!isOpen) {
    return null;
  }

  const analyze = async () => {
    setAnalyzeError("");
    if (!resumeToAnalyze) {
      setAnalyzeError("Keine Arbeitsversion oder gespeicherte Variante verfügbar.");
      return;
    }
    if (jobDescription.trim().length < 100) {
      setAnalyzeError("Bitte mindestens 100 Zeichen der Stellenbeschreibung einfügen.");
      return;
    }
    setIsAnalyzing(true);
    setResult(null);
    try {
      const r = await analyzeAts(resumeToAnalyze, jobDescription, category);
      setResult(r);
    } catch (e) {
      setAnalyzeError(e instanceof Error ? e.message : String(e));
    } finally {
      setIsAnalyzing(false);
    }
  };

  const exportAnyway = async () => {
    await onExportAnyway();
    handleClose();
  };

  return (
    <div className="ats-modal-overlay" onClick={() => void handleClose()} role="presentation">
      <div className="ats-modal" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true">
        <div className="ats-modal-header">
          <h2>🎯 ATS-Score Checker</h2>
          <button type="button" className="ats-modal-close" onClick={() => void handleClose()} aria-label="Schließen">
            ✕
          </button>
        </div>

        <div className="ats-modal-body">
          <div className="category-selector">
            <label>Bewerbungskategorie</label>
            <div className="category-pills">
              <button
                type="button"
                className={`pill ${category === 0 ? "pill-active" : ""}`}
                onClick={() => setCategory(0)}
              >
                🔍 Automatisch erkennen
              </button>
              <button
                type="button"
                className={`pill ${category === 1 ? "pill-active" : ""}`}
                onClick={() => setCategory(1)}
              >
                💻 Software Entwickler
              </button>
              <button
                type="button"
                className={`pill ${category === 2 ? "pill-active" : ""}`}
                onClick={() => setCategory(2)}
              >
                🖥 IT Support
              </button>
              <button
                type="button"
                className={`pill ${category === 3 ? "pill-active" : ""}`}
                onClick={() => setCategory(3)}
              >
                📦 Allgemein / Service
              </button>
            </div>
            {category === 0 && result ? (
              <small className="category-hint">
                Erkannte Kategorie: <strong>{result.categoryLabel}</strong>
              </small>
            ) : null}
          </div>

          <h3>Schritt 1 - Stellenbeschreibung</h3>
          <textarea
            className="form-control"
            rows={7}
            placeholder="Füge hier die Stellenbeschreibung ein... (min. 100 Zeichen)"
            value={jobDescription}
            onChange={(e) => setJobDescription(e.target.value)}
          />
          <small className="muted">
            Zeichen: {jobDescription.length} / empfohlen: 200+
          </small>

          <div className="ats-source-hint">
            Geprüft wird: <span>{sourceHint}</span>
          </div>

          {analyzeError ? (
            <div className="alert alert-danger" role="alert">
              {analyzeError}
            </div>
          ) : null}

          <div className="toolbar-actions">
            <button type="button" className="btn btn-primary btn-sm" onClick={() => void analyze()} disabled={!canAnalyze || isAnalyzing}>
              {isAnalyzing ? "Analysiere..." : "🎯 Jetzt analysieren"}
            </button>
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => void handleClose()}>
              Abbrechen
            </button>
          </div>

          {result ? (
            <>
              <hr />
              <div className="ats-score-display">
                <div className="ats-score-meta">
                  <span className="ats-category-badge">{result.categoryLabel}</span>
                </div>
                <div className="ats-score-number" style={{ color: result.scoreColor }}>
                  {result.score}
                </div>
                <div className="ats-score-label">{result.scoreLabel}</div>
                <div className="ats-progress-bar">
                  <div className="ats-progress-fill" style={{ width: `${result.score}%`, background: result.scoreColor }} />
                </div>
              </div>

              <div className="ats-subscores">
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.hardRequirementsScore}/30</div>
                  <div className="ats-subscore-label">Must-Haves</div>
                </div>
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.keywordScore}/25</div>
                  <div className="ats-subscore-label">Keywords</div>
                </div>
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.evidenceScore}/20</div>
                  <div className="ats-subscore-label">Belege</div>
                </div>
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.completenessScore}/10</div>
                  <div className="ats-subscore-label">Vollständigkeit</div>
                </div>
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.formattingScore}/10</div>
                  <div className="ats-subscore-label">Struktur</div>
                </div>
                <div className="ats-subscore-card">
                  <div className="ats-subscore-value">{result.languageScore}/5</div>
                  <div className="ats-subscore-label">Sprache</div>
                </div>
              </div>

              {result.missingMustHaveKeywords.length > 0 ? (
                <>
                  <h4>🚨 Fehlende Must-Have Keywords ({result.missingMustHaveKeywords.length})</h4>
                  <div className="keyword-chips">
                    {result.missingMustHaveKeywords.slice(0, 12).map((k) => (
                      <span key={k} className="keyword-chip keyword-chip-must">
                        {k}
                      </span>
                    ))}
                  </div>
                </>
              ) : null}

              <h4>✅ Gefundene Tokens (allgemein) ({result.matchedKeywords.length})</h4>
              <div className="keyword-chips">
                {result.matchedKeywords.slice(0, 20).map((k) => (
                  <span key={k} className="keyword-chip keyword-chip-matched">
                    {k}
                  </span>
                ))}
              </div>

              <h4>🎯 Gefundene Skill-Keywords (score-relevant) ({result.matchedSkillKeywords.length})</h4>
              <div className="keyword-chips">
                {result.matchedSkillKeywords.length === 0 ? (
                  <span className="keyword-chip keyword-chip-info">Keine score-relevanten Skill-Keywords erkannt</span>
                ) : (
                  result.matchedSkillKeywords.slice(0, 20).map((k) => (
                    <span key={k} className="keyword-chip keyword-chip-skill">
                      {k}
                    </span>
                  ))
                )}
              </div>

              <h4>❌ Fehlende Keywords ({result.missingKeywords.length})</h4>
              <div className="keyword-chips">
                {result.missingKeywords.slice(0, 20).map((k) => (
                  <span key={k} className="keyword-chip keyword-chip-missing">
                    {k}
                  </span>
                ))}
              </div>

              <h4>📋 Verbesserungsvorschläge</h4>
              {result.improvements.map((improvement, i) => (
                <div key={i} className={`ats-improvement-item ${priorityClass(improvement.priority)}`}>
                  <div className="ats-improvement-icon">{improvement.priorityIcon}</div>
                  <div className="ats-improvement-content">
                    <div className="ats-improvement-top">
                      <span className={`ats-priority-badge ${priorityClass(improvement.priority)}`}>{improvement.priority}</span>
                    </div>
                    <div className="issue">
                      {improvement.category}: {improvement.issue}
                    </div>
                    <div className="suggestion">{improvement.suggestion}</div>
                  </div>
                </div>
              ))}

              <div className="toolbar-actions">
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => void handleClose()}>
                  ✏ CV anpassen
                </button>
                <button type="button" className="btn btn-primary btn-sm" onClick={() => void exportAnyway()}>
                  📄 Trotzdem exportieren
                </button>
              </div>
            </>
          ) : null}
        </div>
      </div>
    </div>
  );
}
