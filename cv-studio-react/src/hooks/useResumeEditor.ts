import { useCallback, useEffect, useRef, useState } from "react";
import * as api from "../api/resumeApi";
import { clearLastResumeId, setLastResumeId } from "../lib/cvStudio";
import { formatVariantenName } from "../lib/formatting";
import { coerceResumeData, normalizeResumeDto } from "../lib/resumeData";
import { ensureSectionTitles } from "../lib/sectionTitles";
import type { PdfDesign, ResumeDto, ResumeSummaryDto, ResumeTemplateDto, ResumeVersionDto } from "../types/cv";

function patchResume(r: ResumeDto): ResumeDto {
  const n = normalizeResumeDto(r);
  ensureSectionTitles(n.resumeData);
  return n;
}

export function useResumeEditor() {
  const [templates, setTemplates] = useState<ResumeTemplateDto[]>([]);
  const [summaries, setSummaries] = useState<ResumeSummaryDto[]>([]);
  const [resume, setResumeState] = useState<ResumeDto | null>(null);
  const [versions, setVersions] = useState<ResumeVersionDto[]>([]);
  const [activeVariant, setActiveVariant] = useState<ResumeVersionDto | null>(null);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState("");
  const [pdfDesign, setPdfDesign] = useState<PdfDesign>("A");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [variantNameDraft, setVariantNameDraft] = useState("");
  const [autoSaving, setAutoSaving] = useState(false);
  const [lastSavedAtUtc, setLastSavedAtUtc] = useState<Date | null>(null);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  const resumeRef = useRef<ResumeDto | null>(null);
  const hasUnsavedRef = useRef(false);
  const selectedTemplateKeyRef = useRef("");
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    resumeRef.current = resume;
  }, [resume]);

  useEffect(() => {
    selectedTemplateKeyRef.current = selectedTemplateKey;
  }, [selectedTemplateKey]);

  const refreshSummaries = useCallback(async () => {
    const list = await api.listResumes();
    setSummaries(list);
  }, []);

  const refreshVersions = useCallback(async (resumeId: string) => {
    const v = await api.listVersions(resumeId);
    setVersions(v);
  }, []);

  const assignResume = useCallback(
    (
      r: ResumeDto | null,
      opts?: { keepDirty?: boolean; activeVar?: ResumeVersionDto | null | undefined },
    ) => {
      const patched = r ? patchResume(r) : null;
      resumeRef.current = patched;
      setResumeState(patched);
      if (opts && "activeVar" in opts) {
        setActiveVariant(opts.activeVar ?? null);
      }
      if (!patched) {
        hasUnsavedRef.current = false;
        setHasUnsavedChanges(false);
        return;
      }
      if (opts?.keepDirty) {
        hasUnsavedRef.current = true;
        setHasUnsavedChanges(true);
      } else {
        hasUnsavedRef.current = false;
        setHasUnsavedChanges(false);
      }
    },
    [],
  );

  const runBusy = useCallback(async <T,>(fn: () => Promise<T>): Promise<T | undefined> => {
    setBusy(true);
    setError(null);
    try {
      return await fn();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);

  const saveNow = useCallback(async () => {
    const r = resumeRef.current;
    if (!r || !hasUnsavedRef.current) {
      return;
    }
    setAutoSaving(true);
    setError(null);
    try {
      const tk = selectedTemplateKeyRef.current || r.templateKey || null;
      const updated = await api.updateResume(r.id, {
        title: r.title,
        templateKey: tk,
        resumeData: coerceResumeData(r.resumeData),
      });
      const patched = patchResume(updated);
      hasUnsavedRef.current = false;
      setHasUnsavedChanges(false);
      setLastSavedAtUtc(new Date());
      resumeRef.current = patched;
      setResumeState(patched);
      await refreshSummaries();
      setLastResumeId(patched.id);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setAutoSaving(false);
    }
  }, [refreshSummaries]);

  const queueAutoSave = useCallback(() => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current);
    }
    saveTimerRef.current = setTimeout(() => {
      void saveNow();
    }, 1000);
  }, [saveNow]);

  const flushAutoSave = useCallback(async () => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current);
      saveTimerRef.current = null;
    }
    await saveNow();
  }, [saveNow]);

  const updateResume = useCallback(
    (recipe: (r: ResumeDto) => void) => {
      setResumeState((prev) => {
        if (!prev) {
          return prev;
        }
        const next = structuredClone(prev);
        recipe(next);
        ensureSectionTitles(next.resumeData);
        resumeRef.current = next;
        hasUnsavedRef.current = true;
        setHasUnsavedChanges(true);
        setActiveVariant(null);
        queueAutoSave();
        return next;
      });
    },
    [queueAutoSave],
  );

  useEffect(
    () => () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current);
      }
    },
    [],
  );

  const loadTemplates = useCallback(async () => {
    await runBusy(async () => {
      const t = await api.getTemplates();
      setTemplates(t);
      const list = await api.listResumes();
      setSummaries(list);
      if (!selectedTemplateKeyRef.current && t.length > 0) {
        setSelectedTemplateKey(t[0].key);
      }
    });
  }, [runBusy]);

  const createArbeitsversion = useCallback(async () => {
    const created = await runBusy(async () => {
      if (selectedTemplateKeyRef.current) {
        return api.createResumeFromTemplate(selectedTemplateKeyRef.current);
      }
      return api.createResume({
        title: "Neue Arbeitsversion",
        resumeData: coerceResumeData(undefined),
      });
    });
    if (!created) {
      return null;
    }
    assignResume(created);
    await refreshVersions(created.id);
    await refreshSummaries();
    setLastResumeId(created.id);
    return created.id;
  }, [assignResume, refreshSummaries, refreshVersions, runBusy]);

  const openResume = useCallback(
    async (id: string) => {
      const loaded = await runBusy(async () => api.getResume(id));
      if (!loaded) {
        return;
      }
      assignResume(loaded);
      if (loaded.templateKey) {
        setSelectedTemplateKey(loaded.templateKey);
      }
      await refreshVersions(loaded.id);
      await refreshSummaries();
      setLastResumeId(loaded.id);
    },
    [assignResume, refreshSummaries, refreshVersions, runBusy],
  );

  const saveVariant = useCallback(
    async (name?: string | null) => {
      const r = resumeRef.current;
      if (!r) {
        return null;
      }
      await flushAutoSave();
      const label = (name ?? variantNameDraft).trim();
      const created = await runBusy(async () =>
        api.createVersion(r.id, { label: label || null }),
      );
      if (!created) {
        return null;
      }
      setVariantNameDraft("");
      assignResume(r, { keepDirty: false, activeVar: created });
      await refreshVersions(r.id);
      await refreshSummaries();
      setLastResumeId(r.id);
      return created;
    },
    [assignResume, flushAutoSave, refreshSummaries, refreshVersions, runBusy, variantNameDraft],
  );

  const loadVariantIntoEditor = useCallback(
    async (versionId: string) => {
      const r = resumeRef.current;
      if (!r) {
        return;
      }
      const variante = await runBusy(async () => api.getVersion(r.id, versionId));
      if (!variante) {
        return;
      }
      const merged: ResumeDto = {
        ...r,
        resumeData: coerceResumeData(variante.resumeData),
        updatedAtUtc: new Date().toISOString(),
      };
      assignResume(merged, { keepDirty: true, activeVar: variante });
      queueAutoSave();
      setLastResumeId(r.id);
    },
    [assignResume, queueAutoSave, runBusy],
  );

  const resetAll = useCallback(async () => {
    await runBusy(async () => {
      await api.deleteAllResumes();
      assignResume(null);
      setVersions([]);
      clearLastResumeId();
      await refreshSummaries();
    });
  }, [assignResume, refreshSummaries, runBusy]);

  const aktivKontextText =
    activeVariant === null ? "Arbeitsversion" : `Gespeicherte Variante ${formatVariantenName(activeVariant)}`;

  const autoSaveText = autoSaving
    ? "Auto-Save: speichert..."
    : hasUnsavedChanges
      ? "Auto-Save: wartend"
      : lastSavedAtUtc
        ? `Auto-Save: gespeichert (${lastSavedAtUtc.toLocaleTimeString()})`
        : "Auto-Save: bereit";

  return {
    templates,
    summaries,
    resume,
    versions,
    activeVariant,
    selectedTemplateKey,
    setSelectedTemplateKey,
    pdfDesign,
    setPdfDesign,
    busy,
    error,
    setError,
    variantNameDraft,
    setVariantNameDraft,
    autoSaving,
    lastSavedAtUtc,
    hasUnsavedChanges,
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
    refreshSummaries,
    refreshVersions,
    assignResume,
  };
}
