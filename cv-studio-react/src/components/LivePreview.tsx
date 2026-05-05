import { CvSectionTitleResolver } from "../lib/sectionTitles";
import { formatUrl } from "../lib/formatting";
import type { PdfDesign, ProfileData, ResumeDto } from "../types/cv";

function hasSocialLinks(profile: ProfileData): boolean {
  return Boolean(profile.linkedInUrl?.trim() || profile.gitHubUrl?.trim() || profile.portfolioUrl?.trim());
}

type Props = {
  resume: ResumeDto | null;
  pdfDesign: PdfDesign;
};

export function LivePreview({ resume, pdfDesign }: Props) {
  if (!resume) {
    return null;
  }
  const d = resume.resumeData;
  const p = d.profile;

  return (
    <div className={`preview-card ${pdfDesign === "B" ? "preview-design-b" : ""}`}>
      <div className="preview-header">
        {p.profileImageUrl?.trim() ? (
          <img className="profile-image" src={p.profileImageUrl} alt="Profilbild" />
        ) : null}
        <div>
          <h3>
            {p.firstName} {p.lastName}
          </h3>
          <p className="muted">{p.headline}</p>
        </div>
      </div>
      <p className="muted">
        {[p.email, p.phone, p.location].filter(Boolean).join(" | ")}
      </p>
      {hasSocialLinks(p) ? (
        <div className="cv-social-links">
          {p.linkedInUrl?.trim() ? (
            <span className="cv-social-link cv-social-li">in {formatUrl(p.linkedInUrl)}</span>
          ) : null}
          {p.gitHubUrl?.trim() ? (
            <span className="cv-social-link cv-social-gh">gh {formatUrl(p.gitHubUrl)}</span>
          ) : null}
          {p.portfolioUrl?.trim() ? (
            <span className="cv-social-link cv-social-web">web {formatUrl(p.portfolioUrl)}</span>
          ) : null}
        </div>
      ) : null}
      {p.workPermit?.trim() ? <div className="cv-work-permit">✓ {p.workPermit}</div> : null}
      {p.summary?.trim() ? (
        <>
          <h4>{CvSectionTitleResolver.qualificationsProfile(d)}</h4>
          <p>{p.summary}</p>
        </>
      ) : null}

      <h4>{CvSectionTitleResolver.workExperience(d)}</h4>
      {d.workItems.length === 0 ? (
        <p className="muted">Keine Eintraege</p>
      ) : (
        d.workItems.map((work, idx) => (
          <article key={idx}>
            <strong>
              {work.role} - {work.company}
            </strong>
            <div className="muted">
              {work.startDate} - {work.endDate}
            </div>
            {work.description?.trim() ? <p>{work.description}</p> : null}
            {work.bullets.length > 0 ? (
              <ul>
                {work.bullets.map((b, i) => (
                  <li key={i}>{b}</li>
                ))}
              </ul>
            ) : null}
          </article>
        ))
      )}

      <h4>{CvSectionTitleResolver.education(d)}</h4>
      {d.educationItems.length === 0 ? (
        <p className="muted">Keine Eintraege</p>
      ) : (
        d.educationItems.map((edu, idx) => (
          <article key={idx}>
            <strong>{edu.degree}</strong>
            <div>{edu.school}</div>
            <div className="muted">
              {edu.startDate} - {edu.endDate}
            </div>
          </article>
        ))
      )}

      <h4>{CvSectionTitleResolver.skills(d)}</h4>
      {d.skills.length === 0 ? (
        <p className="muted">Keine Eintraege</p>
      ) : (
        d.skills.map((skill, idx) => (
          <div key={idx}>
            <strong>{skill.categoryName}:</strong> {skill.items.join(", ")}
          </div>
        ))
      )}

      <h4>{CvSectionTitleResolver.interests(d)}</h4>
      {d.hobbies.length === 0 ? (
        <p className="muted">Keine Eintraege</p>
      ) : (
        <p>{d.hobbies.join(" | ")}</p>
      )}
    </div>
  );
}
