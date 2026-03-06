namespace CvStudio.Application.Services;

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

    public string ScoreLabel => Score switch
    {
        >= 85 => "Sehr gut",
        >= 70 => "Gut",
        >= 55 => "Ausreichend",
        _ => "Verbesserungsbedarf"
    };

    public string ScoreColor => Score switch
    {
        >= 85 => "#16A34A",
        >= 70 => "#D97706",
        >= 55 => "#EA580C",
        _ => "#C9392B"
    };
}