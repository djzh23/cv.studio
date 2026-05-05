import type { CvSectionTitleOverrides, ResumeData } from "../types/cv";

function pick(user: string | null | undefined, fallback: string): string {
  const t = user?.trim();
  return t ? t : fallback;
}

export const CvSectionTitleResolver = {
  qualificationsProfile: (d: ResumeData) =>
    pick(d.sectionTitles?.qualificationsProfile, "Qualifikationsprofil"),
  workExperience: (d: ResumeData) => pick(d.sectionTitles?.workExperience, "Berufserfahrung"),
  education: (d: ResumeData) => pick(d.sectionTitles?.education, "Ausbildung"),
  skills: (d: ResumeData) => pick(d.sectionTitles?.skills, "Kenntnisse"),
  projects: (d: ResumeData) => pick(d.sectionTitles?.projects, "Projekte"),
  languagesAndInterests: (d: ResumeData) =>
    pick(d.sectionTitles?.languagesAndInterests, "Sprachen & Interessen"),
  contacts: (d: ResumeData) => pick(d.sectionTitles?.contacts, "Kontakte"),
  languages: (d: ResumeData) => pick(d.sectionTitles?.languages, "Sprachen"),
  interests: (d: ResumeData) => pick(d.sectionTitles?.interests, "Interessen"),
};

export function ensureSectionTitles(d: ResumeData): CvSectionTitleOverrides {
  if (!d.sectionTitles) {
    d.sectionTitles = {};
  }
  return d.sectionTitles;
}
