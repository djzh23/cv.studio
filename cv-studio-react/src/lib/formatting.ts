import type { ResumeVersionDto } from "../types/cv";

export function formatVariantenName(v: ResumeVersionDto): string {
  const basis = v.label?.trim() ? v.label.trim() : "Ohne Namen";
  return `${basis} v${v.versionNumber}`;
}

export function templateIconClass(templateKey: string): string {
  switch (templateKey) {
    case "softwareentwickler":
      return "bi bi-code-slash";
    case "it-support":
      return "bi bi-headset";
    case "service-gastro-zustellung":
      return "bi bi-briefcase";
    default:
      return "bi bi-file-earmark-text";
  }
}

export function versionBadgeClass(versionNumber: number): string {
  const normalized = Math.max(versionNumber, 1);
  const index = (normalized - 1) % 3;
  if (index === 0) {
    return "badge-v1";
  }
  if (index === 1) {
    return "badge-v2";
  }
  return "badge-v3";
}

export function formatUrl(url: string | null | undefined): string {
  if (!url?.trim()) {
    return "";
  }
  return url
    .replace(/^https:\/\//i, "")
    .replace(/^http:\/\//i, "")
    .replace(/\/$/, "")
    .trim();
}
