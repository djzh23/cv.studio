import type {
  AtsScoreResult,
  CreateResumeRequest,
  CreateVersionRequest,
  JobCategory,
  PdfDesign,
  ResumeData,
  ResumeDto,
  ResumeSummaryDto,
  ResumeTemplateDto,
  ResumeVersionDto,
  UpdateResumeRequest,
  UpdateVersionRequest,
} from "../types/cv";

const jsonHeaders = { "Content-Type": "application/json", Accept: "application/json" };

async function readError(res: Response): Promise<string> {
  const text = await res.text();
  if (!text) {
    return res.statusText || "API request failed.";
  }
  try {
    const j = JSON.parse(text) as { detail?: string; title?: string };
    if (j.detail) {
      return j.detail;
    }
    if (j.title) {
      return j.title;
    }
  } catch {
    /* ignore */
  }
  return text;
}

async function parseJson<T>(res: Response): Promise<T> {
  if (!res.ok) {
    throw new Error(await readError(res));
  }
  const data = (await res.json()) as T;
  return data;
}

export async function verifyAccess(passcode: string): Promise<void> {
  const res = await fetch("/api/access/verify", {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify({ passcode }),
  });
  if (res.status === 204) {
    return;
  }
  throw new Error(await readError(res));
}

export async function analyzeAts(
  resumeData: ResumeData,
  jobDescription: string,
  category: JobCategory,
): Promise<AtsScoreResult> {
  const res = await fetch("/api/ats/analyze", {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify({ resumeData, jobDescription, category }),
  });
  return parseJson<AtsScoreResult>(res);
}

export async function getTemplates(): Promise<ResumeTemplateDto[]> {
  const res = await fetch("/api/resume-templates");
  return parseJson<ResumeTemplateDto[]>(res);
}

export async function listResumes(): Promise<ResumeSummaryDto[]> {
  const res = await fetch("/api/resumes");
  return parseJson<ResumeSummaryDto[]>(res);
}

export async function deleteAllResumes(): Promise<void> {
  const res = await fetch("/api/resumes", { method: "DELETE" });
  if (!res.ok) {
    throw new Error(await readError(res));
  }
}

export async function createResume(req: CreateResumeRequest): Promise<ResumeDto> {
  const res = await fetch("/api/resumes", {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(req),
  });
  return parseJson<ResumeDto>(res);
}

export async function createResumeFromTemplate(templateKey: string): Promise<ResumeDto> {
  const res = await fetch(`/api/resumes/templates/${encodeURIComponent(templateKey)}`, {
    method: "POST",
  });
  return parseJson<ResumeDto>(res);
}

export async function getResume(id: string): Promise<ResumeDto> {
  const res = await fetch(`/api/resumes/${id}`);
  return parseJson<ResumeDto>(res);
}

export async function updateResume(id: string, req: UpdateResumeRequest): Promise<ResumeDto> {
  const res = await fetch(`/api/resumes/${id}`, {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(req),
  });
  return parseJson<ResumeDto>(res);
}

export async function createVersion(id: string, req: CreateVersionRequest): Promise<ResumeVersionDto> {
  const res = await fetch(`/api/resumes/${id}/versions`, {
    method: "POST",
    headers: jsonHeaders,
    body: JSON.stringify(req),
  });
  return parseJson<ResumeVersionDto>(res);
}

export async function listVersions(id: string): Promise<ResumeVersionDto[]> {
  const res = await fetch(`/api/resumes/${id}/versions`);
  return parseJson<ResumeVersionDto[]>(res);
}

export async function getVersion(resumeId: string, versionId: string): Promise<ResumeVersionDto> {
  const res = await fetch(`/api/resumes/${resumeId}/versions/${versionId}`);
  return parseJson<ResumeVersionDto>(res);
}

export async function updateVersion(
  resumeId: string,
  versionId: string,
  req: UpdateVersionRequest,
): Promise<ResumeVersionDto> {
  const res = await fetch(`/api/resumes/${resumeId}/versions/${versionId}`, {
    method: "PUT",
    headers: jsonHeaders,
    body: JSON.stringify(req),
  });
  return parseJson<ResumeVersionDto>(res);
}

export async function deleteVersion(resumeId: string, versionId: string): Promise<void> {
  const res = await fetch(`/api/resumes/${resumeId}/versions/${versionId}`, { method: "DELETE" });
  if (!res.ok) {
    throw new Error(await readError(res));
  }
}

function designQuery(design: PdfDesign): string {
  const q = design === "B" ? "B" : design === "C" ? "C" : "A";
  return q;
}

export async function downloadPdf(
  resumeId: string,
  options?: { versionId?: string | null; design?: PdfDesign },
): Promise<Blob> {
  const params = new URLSearchParams();
  params.set("design", designQuery(options?.design ?? "A"));
  if (options?.versionId) {
    params.set("versionId", options.versionId);
  }
  const res = await fetch(`/api/resumes/${resumeId}/pdf?${params.toString()}`);
  if (!res.ok) {
    throw new Error(await readError(res));
  }
  return res.blob();
}

export async function downloadDocx(resumeId: string, versionId?: string | null): Promise<Blob> {
  const params = new URLSearchParams();
  if (versionId) {
    params.set("versionId", versionId);
  }
  const qs = params.toString();
  const res = await fetch(`/api/resumes/${resumeId}/docx${qs ? `?${qs}` : ""}`);
  if (!res.ok) {
    throw new Error(await readError(res));
  }
  return res.blob();
}
