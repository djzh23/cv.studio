import type { ProfileData, ResumeData } from "../types/cv";

const emptyProfile = (): ProfileData => ({
  firstName: "",
  lastName: "",
  headline: "",
  email: "",
  phone: "",
  location: "",
  profileImageUrl: "",
  gitHubUrl: "",
  linkedInUrl: "",
  portfolioUrl: "",
  workPermit: "",
  summary: "",
});

/** Normalisiert API-Payload zu einem vollstaendigen ResumeData (fehlende Arrays). */
export function coerceResumeData(raw: ResumeData | undefined | null): ResumeData {
  const r = raw ?? ({} as ResumeData);
  return {
    profile: { ...emptyProfile(), ...r.profile },
    workItems: r.workItems ?? [],
    educationItems: r.educationItems ?? [],
    projects: r.projects ?? [],
    skills: r.skills ?? [],
    hobbies: r.hobbies ?? [],
    sectionTitles: r.sectionTitles ? { ...r.sectionTitles } : {},
  };
}

export function normalizeResumeDto<T extends { resumeData: ResumeData }>(dto: T): T {
  return {
    ...dto,
    resumeData: coerceResumeData(dto.resumeData),
  };
}
