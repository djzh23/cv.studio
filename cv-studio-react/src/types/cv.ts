export type PdfDesign = "A" | "B" | "C";

/** Matches CvStudio.Application.Services.JobCategory (JSON numeric). */
export type JobCategory = 0 | 1 | 2 | 3;

export interface CvSectionTitleOverrides {
  qualificationsProfile?: string | null;
  workExperience?: string | null;
  education?: string | null;
  skills?: string | null;
  projects?: string | null;
  languagesAndInterests?: string | null;
  contacts?: string | null;
  languages?: string | null;
  interests?: string | null;
  languagesInlineLabel?: string | null;
  interestsInlineLabel?: string | null;
  designBLanguagesRowLabel?: string | null;
  designBInterestsRowLabel?: string | null;
}

export interface ProfileData {
  firstName: string;
  lastName: string;
  headline: string;
  email: string;
  phone: string;
  location: string;
  profileImageUrl: string;
  gitHubUrl?: string | null;
  linkedInUrl?: string | null;
  portfolioUrl?: string | null;
  workPermit?: string | null;
  summary: string;
}

export interface WorkItemData {
  company: string;
  role: string;
  startDate: string;
  endDate: string;
  description: string;
  bullets: string[];
}

export interface EducationItemData {
  school: string;
  degree: string;
  startDate: string;
  endDate: string;
}

export interface SkillGroupData {
  categoryName: string;
  items: string[];
}

export interface ResumeProjectItem {
  name: string;
  description: string;
  technologies: string[];
}

export interface ResumeData {
  profile: ProfileData;
  workItems: WorkItemData[];
  educationItems: EducationItemData[];
  projects: ResumeProjectItem[];
  skills: SkillGroupData[];
  hobbies: string[];
  sectionTitles?: CvSectionTitleOverrides | null;
}

export interface ResumeDto {
  id: string;
  title: string;
  templateKey?: string | null;
  resumeData: ResumeData;
  updatedAtUtc: string;
}

export interface ResumeSummaryDto {
  id: string;
  title: string;
  templateKey?: string | null;
  updatedAtUtc: string;
}

export interface ResumeVersionDto {
  id: string;
  resumeId: string;
  versionNumber: number;
  label?: string | null;
  resumeData: ResumeData;
  createdAtUtc: string;
}

export interface ResumeTemplateDto {
  key: string;
  displayName: string;
  description: string;
}

export interface CreateResumeRequest {
  title: string;
  templateKey?: string | null;
  resumeData: ResumeData;
}

export interface UpdateResumeRequest {
  title: string;
  templateKey?: string | null;
  resumeData: ResumeData;
}

export interface CreateVersionRequest {
  label?: string | null;
}

export interface UpdateVersionRequest {
  label?: string | null;
}

export interface AtsImprovement {
  category: string;
  issue: string;
  suggestion: string;
  priority: string;
  priorityColor?: string;
  priorityIcon?: string;
}

export interface AtsScoreResult {
  detectedCategory: JobCategory;
  score: number;
  hardRequirementsScore: number;
  keywordScore: number;
  evidenceScore: number;
  completenessScore: number;
  formattingScore: number;
  languageScore: number;
  matchedKeywords: string[];
  matchedSkillKeywords: string[];
  missingKeywords: string[];
  missingMustHaveKeywords: string[];
  improvements: AtsImprovement[];
  scoreLabel?: string;
  scoreColor?: string;
  categoryLabel?: string;
}
