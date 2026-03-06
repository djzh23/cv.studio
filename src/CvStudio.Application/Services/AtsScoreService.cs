using System.Text;
using System.Text.RegularExpressions;
using CvStudio.Application.Contracts;

namespace CvStudio.Application.Services;

public sealed class AtsScoreService : IAtsScoreService
{
    private static readonly HashSet<string> GermanStopwords =
    [
        "und", "oder", "mit", "für", "von", "in", "an", "auf", "bei",
        "ist", "sind", "wird", "haben", "wir", "sie", "das", "die",
        "der", "ein", "eine", "zu", "im", "dem", "den", "des", "auch",
        "als", "nach", "wenn", "dann", "aber", "noch", "schon", "nur"
    ];

    private static readonly HashSet<string> EnglishStopwords =
    [
        "and", "or", "with", "for", "the", "a", "an", "in", "at", "is",
        "are", "will", "we", "you", "that", "this", "of", "to", "be",
        "have", "has", "was", "were", "from", "by", "on", "as"
    ];

    public AtsScoreResult Calculate(ResumeData resume, string jobDescription)
    {
        return Calculate(resume, jobDescription, JobCategory.Auto);
    }

    public AtsScoreResult Calculate(ResumeData resume, string jobDescription, JobCategory category)
    {
        if (resume is null)
        {
            throw new ArgumentNullException(nameof(resume));
        }

        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            throw new ArgumentException("Job description is required.", nameof(jobDescription));
        }

        var effectiveCategory = category == JobCategory.Auto
            ? DetectCategory(jobDescription)
            : category;

        var profile = CategoryProfiles.Get(effectiveCategory);

        var result = new AtsScoreResult
        {
            DetectedCategory = effectiveCategory,
            KeywordScore = CalcKeywords(resume, jobDescription),
            CompletenessScore = CalcCompleteness(resume),
            FormattingScore = CalcFormatting(resume),
            LanguageScore = CalcLanguage(resume, profile)
        };

        result.Score = Math.Min(100, result.KeywordScore + result.CompletenessScore + result.FormattingScore + result.LanguageScore);
        result.MatchedKeywords = GetMatchedKeywords(resume, jobDescription);
        result.MissingKeywords = GetMissingKeywords(resume, jobDescription, result.MatchedKeywords);
        result.Improvements = BuildImprovements(result, resume, profile, effectiveCategory);
        return result;
    }

    private static JobCategory DetectCategory(string jobDesc)
    {
        var text = jobDesc.ToLowerInvariant();

        string[] softwareSignals =
        [
            "entwickler", "developer", "software", "programmier",
            "csharp", "dotnet", "java", "python", "react", "angular",
            "blazor", "frontend", "backend", "fullstack", "devops",
            "architektur", "clean architecture", "microservices",
            "api", "datenbank", "repository", "deployment", "git"
        ];

        string[] itSupportSignals =
        [
            "it-support", "helpdesk", "first level", "second level",
            "ticketsystem", "jira", "servicenow", "itil", "netzwerk",
            "windows", "active directory", "troubleshooting",
            "hardware", "infrastruktur", "support", "incident",
            "fernwartung", "remotedesktop", "patch", "rollout"
        ];

        string[] allgemeinSignals =
        [
            "servicekraft", "küche", "koch", "gastronomie", "gastro",
            "kommissionierung", "zustellung", "logistik", "lager",
            "fahrer", "pakete", "briefe", "post", "sortierung",
            "reinigung", "haushalt", "pflege", "einzelhandel",
            "verkauf", "kasse", "kundendienst", "rezeption"
        ];

        var swScore = softwareSignals.Count(text.Contains);
        var itScore = itSupportSignals.Count(text.Contains);
        var agScore = allgemeinSignals.Count(text.Contains);

        if (swScore == 0 && itScore == 0 && agScore == 0)
        {
            return JobCategory.Allgemein;
        }

        if (swScore >= itScore && swScore >= agScore)
        {
            return JobCategory.SoftwareEntwickler;
        }

        if (itScore >= swScore && itScore >= agScore)
        {
            return JobCategory.ItSupport;
        }

        return JobCategory.Allgemein;
    }

    private static int CalcKeywords(ResumeData cv, string jobDesc)
    {
        var jobTokens = Tokenize(jobDesc)
            .Where(t => t.Length > 3)
            .Where(t => !GermanStopwords.Contains(t) && !EnglishStopwords.Contains(t))
            .ToHashSet();

        if (jobTokens.Count == 0)
        {
            return 0;
        }

        var cvText = GetAllCvText(cv);
        var matched = jobTokens.Count(t => cvText.Contains(t, StringComparison.OrdinalIgnoreCase));
        var ratio = (double)matched / jobTokens.Count;
        return (int)Math.Min(40, Math.Round(ratio * 40, MidpointRounding.AwayFromZero));
    }

    private static int CalcCompleteness(ResumeData cv)
    {
        var profile = cv.Profile;
        var score = 0;

        if (!string.IsNullOrWhiteSpace(profile.FirstName)) score += 2;
        if (!string.IsNullOrWhiteSpace(profile.LastName)) score += 2;
        if (!string.IsNullOrWhiteSpace(profile.Headline)) score += 3;
        if (!string.IsNullOrWhiteSpace(profile.Summary) && profile.Summary.Length > 50) score += 4;
        if (!string.IsNullOrWhiteSpace(profile.Email)) score += 2;
        if (!string.IsNullOrWhiteSpace(profile.Phone)) score += 2;
        if (cv.WorkItems.Count >= 1) score += 5;
        if (cv.EducationItems.Count >= 1) score += 3;
        if (cv.Skills.Count >= 1) score += 2;

        return Math.Min(25, score);
    }

    private static int CalcFormatting(ResumeData cv)
    {
        var score = 0;
        var summary = cv.Profile.Summary;

        if (!string.IsNullOrWhiteSpace(summary) && summary.Length > 80)
        {
            score += 5;
        }

        var hasBullets = cv.WorkItems.Count > 0 && cv.WorkItems.All(e =>
            e.Bullets.Count > 0 ||
            e.Description.Contains("•", StringComparison.Ordinal) ||
            e.Description.Contains("-", StringComparison.Ordinal) ||
            e.Description.Contains('\n', StringComparison.Ordinal));
        if (hasBullets)
        {
            score += 5;
        }

        const string datePattern = @"^(\d{2}/\d{4}|heute|aktuell)$";
        var datesAreConsistent = cv.WorkItems.Count > 0 && cv.WorkItems.All(e =>
            Regex.IsMatch((e.StartDate ?? string.Empty).Trim(), datePattern, RegexOptions.IgnoreCase) &&
            Regex.IsMatch((e.EndDate ?? string.Empty).Trim(), datePattern, RegexOptions.IgnoreCase));
        if (datesAreConsistent)
        {
            score += 5;
        }

        var cvText = GetAllCvText(cv);
        var noUmlautErrors =
            !cvText.Contains("ae", StringComparison.OrdinalIgnoreCase) &&
            !cvText.Contains("oe", StringComparison.OrdinalIgnoreCase) &&
            !cvText.Contains("ue", StringComparison.OrdinalIgnoreCase);
        if (noUmlautErrors)
        {
            score += 5;
        }

        return Math.Min(20, score);
    }

    private static int CalcLanguage(ResumeData cv, CategoryProfile profile)
    {
        var score = 0;
        var cvText = GetAllCvText(cv);

        if (profile.StrongVerbs.Any(v => cvText.Contains(v, StringComparison.OrdinalIgnoreCase)))
        {
            score += 5;
        }

        var hasMetrics = profile.MetricPatterns.Any(pattern => Regex.IsMatch(cvText, pattern, RegexOptions.IgnoreCase));
        if (hasMetrics)
        {
            score += 5;
        }

        var genericCount = profile.GenericPhrases.Count(p => cvText.Contains(p, StringComparison.OrdinalIgnoreCase));
        if (genericCount <= 1)
        {
            score += 5;
        }

        return Math.Min(15, score);
    }

    private static List<string> GetMatchedKeywords(ResumeData cv, string jobDesc)
    {
        var jobTokens = Tokenize(jobDesc)
            .Where(t => t.Length > 3)
            .Where(t => !GermanStopwords.Contains(t) && !EnglishStopwords.Contains(t))
            .Distinct()
            .ToList();

        var cvText = GetAllCvText(cv);
        return jobTokens
            .Where(t => cvText.Contains(t, StringComparison.OrdinalIgnoreCase))
            .Take(30)
            .ToList();
    }

    private static List<string> GetMissingKeywords(ResumeData cv, string jobDesc, IReadOnlyCollection<string> matched)
    {
        var matchedSet = matched.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Tokenize(jobDesc)
            .Where(t => t.Length > 3)
            .Where(t => !GermanStopwords.Contains(t) && !EnglishStopwords.Contains(t))
            .Distinct()
            .Where(t => !matchedSet.Contains(t))
            .Take(30)
            .ToList();
    }

    private static List<AtsImprovement> BuildImprovements(
        AtsScoreResult result,
        ResumeData resume,
        CategoryProfile profile,
        JobCategory category)
    {
        var improvements = new List<AtsImprovement>();

        if (result.KeywordScore < 28)
        {
            var missing = result.MissingKeywords.Take(5).ToList();
            var techMissing = missing
                .Where(k => profile.RequiredSkillKeywords.Contains(k, StringComparer.OrdinalIgnoreCase))
                .ToList();
            var otherMissing = missing.Except(techMissing, StringComparer.OrdinalIgnoreCase).ToList();

            if (techMissing.Count > 0)
            {
                improvements.Add(new AtsImprovement
                {
                    Category = "Keywords",
                    Issue = "Wichtige Fachbegriffe fehlen.",
                    Suggestion = $"Ergänze: {string.Join(", ", techMissing)}. {profile.SkillsHint}",
                    Priority = "Hoch"
                });
            }

            if (otherMissing.Count > 0)
            {
                improvements.Add(new AtsImprovement
                {
                    Category = "Keywords",
                    Issue = "Weitere Begriffe aus der Stelle fehlen.",
                    Suggestion = $"Erwähne: {string.Join(", ", otherMissing)}",
                    Priority = "Mittel"
                });
            }
        }

        if (result.CompletenessScore < 18)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Vollständigkeit",
                Issue = "Der CV ist in zentralen Bereichen unvollständig.",
                Suggestion = profile.SummaryHint,
                Priority = "Hoch"
            });
        }

        if (result.FormattingScore < 12)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Formatierung",
                Issue = "Struktur und Datumsangaben sind nicht konsistent genug.",
                Suggestion = profile.ExperienceHint,
                Priority = "Mittel"
            });
        }

        var summaryLen = resume.Profile.Summary?.Length ?? 0;
        if (summaryLen < 100)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Zusammenfassung",
                Issue = $"Zusammenfassung zu kurz ({summaryLen} Zeichen).",
                Suggestion = profile.SummaryHint,
                Priority = summaryLen < 50 ? "Hoch" : "Mittel"
            });
        }

        if (resume.WorkItems.Count == 0)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Berufserfahrung",
                Issue = "Keine Berufserfahrung eingetragen.",
                Suggestion = profile.ExperienceHint,
                Priority = "Hoch"
            });
        }
        else if (result.LanguageScore < 10)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Berufserfahrung",
                Issue = "Beschreibungen zu generisch.",
                Suggestion = profile.ExperienceHint,
                Priority = "Mittel"
            });
        }

        var cvText = GetAllCvText(resume);
        var missingRequired = profile.RequiredSkillKeywords
            .Where(k => !cvText.Contains(k, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();
        if (missingRequired.Count > 0)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Pflicht-Skills",
                Issue = $"Typische Skills für {CategoryLabel(category)} fehlen.",
                Suggestion = $"Ergänze in Kenntnisse: {string.Join(", ", missingRequired)}",
                Priority = "Mittel"
            });
        }

        if (string.IsNullOrWhiteSpace(resume.Profile.ProfileImageUrl))
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Profil",
                Issue = "Kein Profilbild hinterlegt.",
                Suggestion = "In Deutschland wird ein Bewerbungsfoto erwartet.",
                Priority = "Niedrig"
            });
        }

        if (!improvements.Any(i => i.Priority == "Hoch"))
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Gesamt",
                Issue = "CV ist bereits gut aufgebaut.",
                Suggestion = "Passe Keywords noch enger auf diese konkrete Stelle an.",
                Priority = "Niedrig"
            });
        }

        return improvements
            .OrderBy(i => i.Priority switch
            {
                "Hoch" => 0,
                "Mittel" => 1,
                "Niedrig" => 2,
                _ => 3
            })
            .ToList();
    }

    private static string CategoryLabel(JobCategory category) => category switch
    {
        JobCategory.SoftwareEntwickler => "Software-Entwicklung",
        JobCategory.ItSupport => "IT-Support",
        _ => "Service / Logistik"
    };

    private static string GetAllCvText(ResumeData cv)
    {
        var sb = new StringBuilder();
        var profile = cv.Profile;

        sb.Append(profile.FirstName).Append(' ')
            .Append(profile.LastName).Append(' ')
            .Append(profile.Headline).Append(' ')
            .Append(profile.Summary).Append(' ')
            .Append(profile.Email).Append(' ')
            .Append(profile.Location).Append(' ');

        foreach (var work in cv.WorkItems)
        {
            sb.Append(work.Role).Append(' ')
                .Append(work.Company).Append(' ')
                .Append(work.Description).Append(' ')
                .Append(work.StartDate).Append(' ')
                .Append(work.EndDate).Append(' ');

            foreach (var bullet in work.Bullets)
            {
                sb.Append(bullet).Append(' ');
            }
        }

        foreach (var education in cv.EducationItems)
        {
            sb.Append(education.Degree).Append(' ')
                .Append(education.School).Append(' ')
                .Append(education.StartDate).Append(' ')
                .Append(education.EndDate).Append(' ');
        }

        foreach (var skill in cv.Skills)
        {
            sb.Append(skill.CategoryName).Append(' ');
            foreach (var item in skill.Items)
            {
                sb.Append(item).Append(' ');
            }
        }

        foreach (var hobby in cv.Hobbies)
        {
            sb.Append(hobby).Append(' ');
        }

        return sb.ToString().Trim().ToLowerInvariant();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var normalized = Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9äöüß]+", " ");
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
