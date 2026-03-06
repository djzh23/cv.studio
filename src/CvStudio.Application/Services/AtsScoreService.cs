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
        if (resume is null)
        {
            throw new ArgumentNullException(nameof(resume));
        }

        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            throw new ArgumentException("Job description is required.", nameof(jobDescription));
        }

        var result = new AtsScoreResult
        {
            KeywordScore = CalcKeywords(resume, jobDescription),
            CompletenessScore = CalcCompleteness(resume),
            FormattingScore = CalcFormatting(resume),
            LanguageScore = CalcLanguage(resume)
        };

        result.Score = Math.Min(100, result.KeywordScore + result.CompletenessScore + result.FormattingScore + result.LanguageScore);
        result.MatchedKeywords = GetMatchedKeywords(resume, jobDescription);
        result.MissingKeywords = GetMissingKeywords(resume, jobDescription, result.MatchedKeywords);
        result.Improvements = BuildImprovements(result, resume);

        return result;
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

    private static int CalcLanguage(ResumeData cv)
    {
        var score = 0;
        var cvText = GetAllCvText(cv);

        string[] strongVerbs =
        [
            "entwickelt", "koordiniert", "implementiert", "optimiert", "verantwortlich", "durchgeführt",
            "erreicht", "verbessert", "geleitet", "aufgebaut", "designed", "managed", "delivered", "reduced"
        ];

        if (strongVerbs.Any(v => cvText.Contains(v, StringComparison.OrdinalIgnoreCase)))
        {
            score += 5;
        }

        if (Regex.IsMatch(cvText, @"\d+\s*(%|euro|€|kunden|nutzer|team)", RegexOptions.IgnoreCase))
        {
            score += 5;
        }

        string[] genericPhrases = ["teamfähig", "motiviert", "kommunikativ", "flexibel", "zuverlässig"];
        var genericCount = genericPhrases.Count(p => cvText.Contains(p, StringComparison.OrdinalIgnoreCase));
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

    private static List<AtsImprovement> BuildImprovements(AtsScoreResult result, ResumeData resume)
    {
        var improvements = new List<AtsImprovement>();

        if (result.KeywordScore < 28)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Keywords",
                Issue = "Wichtige Keywords aus der Stellenbeschreibung fehlen.",
                Suggestion = result.MissingKeywords.Count > 0
                    ? $"Füge relevante Begriffe ein: {string.Join(", ", result.MissingKeywords.Take(4))}."
                    : "Ergänze fehlende Fachbegriffe im Profil und in der Berufserfahrung.",
                Priority = "Hoch"
            });
        }

        if (result.CompletenessScore < 18)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Vollständigkeit",
                Issue = "Der CV ist in zentralen Bereichen unvollständig.",
                Suggestion = "Ergänze Kontaktdaten, eine aussagekräftige Zusammenfassung sowie Berufs- und Ausbildungsstationen.",
                Priority = "Hoch"
            });
        }

        if (result.FormattingScore < 12)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Formatierung",
                Issue = "Struktur und Datumsangaben sind nicht konsistent genug.",
                Suggestion = "Nutze konsistente Datumsformate (MM/YYYY) und Bullet-Points pro Station.",
                Priority = "Mittel"
            });
        }

        if (result.LanguageScore < 10)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Sprache",
                Issue = "Aussagen sind zu generisch oder enthalten zu wenige Kennzahlen.",
                Suggestion = "Nutze stärkere Verben und ergänze messbare Ergebnisse (%, Anzahl, Zeitersparnis).",
                Priority = "Mittel"
            });
        }

        if (improvements.Count == 0)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Gesamt",
                Issue = "Dein CV ist bereits ATS-freundlich aufgebaut.",
                Suggestion = "Feinschliff: passe Keywords noch enger auf die konkrete Stellenbeschreibung an.",
                Priority = "Niedrig"
            });
        }

        if (resume.WorkItems.Count == 0)
        {
            improvements.Add(new AtsImprovement
            {
                Category = "Berufserfahrung",
                Issue = "Keine Berufserfahrung eingetragen.",
                Suggestion = "Füge mindestens eine relevante Station hinzu.",
                Priority = "Hoch"
            });
        }

        return improvements;
    }

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

public sealed class AtsScoreResult
{
    public int Score { get; set; }
    public int KeywordScore { get; set; }
    public int CompletenessScore { get; set; }
    public int FormattingScore { get; set; }
    public int LanguageScore { get; set; }
    public List<string> MatchedKeywords { get; set; } = [];
    public List<string> MissingKeywords { get; set; } = [];
    public List<AtsImprovement> Improvements { get; set; } = [];
}

public sealed class AtsImprovement
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public string Priority { get; set; } = "Mittel";
}