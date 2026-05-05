const LAST_RESUME_KEY = "resumeVersioner.lastResumeId";
const ACCESS_KEY = "cvstudio.access.granted";

export function getLastResumeId(): string | null {
  return window.localStorage.getItem(LAST_RESUME_KEY);
}

export function setLastResumeId(resumeId: string): void {
  window.localStorage.setItem(LAST_RESUME_KEY, resumeId);
}

export function clearLastResumeId(): void {
  window.localStorage.removeItem(LAST_RESUME_KEY);
}

export function getAccessGranted(): boolean {
  return window.sessionStorage.getItem(ACCESS_KEY) === "1";
}

export function setAccessGranted(granted: boolean): void {
  if (granted) {
    window.sessionStorage.setItem(ACCESS_KEY, "1");
  } else {
    window.sessionStorage.removeItem(ACCESS_KEY);
  }
}

export function clearAccessGranted(): void {
  window.sessionStorage.removeItem(ACCESS_KEY);
}

export function notify(message: string): void {
  window.alert(message);
}

export function downloadBlob(fileName: string, blob: Blob): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
