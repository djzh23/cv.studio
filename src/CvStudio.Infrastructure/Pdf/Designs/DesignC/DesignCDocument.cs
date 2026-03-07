using System.Globalization;
using CvStudio.Application.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CvStudio.Infrastructure.Pdf.Designs.DesignC;

public sealed class DesignCDocument : IDocument
{
    private readonly ResumeData _data;
    private readonly byte[]? _profileImageBytes;
    private readonly ProfileData _profile;
    private readonly IReadOnlyList<WorkItemData> _workItems;
    private readonly IReadOnlyList<EducationItemData> _educationItems;
    private readonly IReadOnlyList<ResumeProjectItem> _projects;
    private readonly IReadOnlyList<SkillGroupData> _skills;

    public DesignCDocument(ResumeData data, byte[]? profileImageBytes)
    {
        _data = data ?? new ResumeData();
        _profileImageBytes = profileImageBytes;
        _profile = _data.Profile ?? new ProfileData();
        _workItems = _data.WorkItems ?? [];
        _educationItems = _data.EducationItems ?? [];
        _projects = _data.Projects ?? [];
        _skills = _data.Skills ?? [];
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(x =>
                x.FontFamily("Arial")
                 .FontSize(DesignCStyles.BodyText)
                 .FontColor(DesignCStyles.MainText));

            page.Content().Row(row =>
            {
                row.ConstantItem(DesignCStyles.SidebarWidth)
                    .Background(DesignCStyles.SidebarBg)
                    .Column(ComposeSidebar);

                row.RelativeItem()
                    .Background(DesignCStyles.MainBg)
                    .Column(ComposeMain);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor col)
    {
        col.Item().Height(20);
        ComposePhotoWithAccent(col);
        col.Item().Height(16);

        ComposeSidebarSectionLabel(col, "\u2709", "CONTACTS");
        col.Item().PaddingHorizontal(16).Column(inner =>
        {
            if (!string.IsNullOrWhiteSpace(_profile.Phone))
            {
                inner.Item().Text(_profile.Phone.Trim())
                    .FontSize(DesignCStyles.SidebarBody)
                    .FontColor(DesignCStyles.SidebarText);
                inner.Item().Height(3);
            }

            if (!string.IsNullOrWhiteSpace(_profile.Email))
            {
                inner.Item().Text(_profile.Email.Trim())
                    .FontSize(DesignCStyles.SidebarBody)
                    .FontColor(DesignCStyles.SidebarText);
                inner.Item().Height(3);
            }

            if (!string.IsNullOrWhiteSpace(_profile.Location))
            {
                inner.Item().Text(_profile.Location.Trim())
                    .FontSize(DesignCStyles.SidebarBody)
                    .FontColor(DesignCStyles.SidebarText);
            }
        });

        col.Item().Height(14);

        if (!string.IsNullOrWhiteSpace(_profile.Summary))
        {
            ComposeSidebarSectionLabel(col, "\u2630", "SUMMARY");
            col.Item().PaddingHorizontal(16)
                .Text(_profile.Summary.Trim())
                .FontSize(DesignCStyles.SidebarBody)
                .FontColor(DesignCStyles.SidebarMuted)
                .LineHeight(1.4f);
            col.Item().Height(14);
        }

        var languages = ResolveLanguages();
        if (languages.Count > 0)
        {
            ComposeSidebarSectionLabel(col, "\u25C9", "LANGUAGES");
            col.Item().PaddingHorizontal(16).Column(inner =>
            {
                foreach (var lang in languages)
                {
                    inner.Item().Row(r =>
                    {
                        r.RelativeItem()
                            .Text(lang.Name.ToUpperInvariant())
                            .FontSize(DesignCStyles.SidebarLabel)
                            .FontColor(DesignCStyles.SidebarText)
                            .Bold();

                        r.ConstantItem(52).AlignRight()
                            .Text(GetLanguageDots(lang.Level))
                            .FontSize(9)
                            .FontColor(DesignCStyles.Cyan);
                    });
                    inner.Item().Height(4);
                }
            });
            col.Item().Height(14);
        }

        var skillGroups = _skills
            .Where(g => !(g.CategoryName ?? string.Empty).Contains("sprach", StringComparison.OrdinalIgnoreCase))
            .Where(g => (g.Items ?? []).Count > 0)
            .ToList();

        if (skillGroups.Count > 0)
        {
            ComposeSidebarSectionLabel(col, "\u2605", "SKILLS");
            col.Item().PaddingHorizontal(16).Column(inner =>
            {
                foreach (var group in skillGroups)
                {
                    inner.Item()
                        .Text((group.CategoryName ?? string.Empty).Trim())
                        .FontSize(DesignCStyles.SidebarLabel)
                        .FontColor(DesignCStyles.SidebarText)
                        .Bold();
                    inner.Item().Height(2);

                    var items = (group.Items ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim());
                    inner.Item()
                        .Text(string.Join(" \u00B7 ", items))
                        .FontSize(DesignCStyles.SidebarBody)
                        .FontColor(DesignCStyles.SidebarMuted)
                        .LineHeight(1.35f);
                    inner.Item().Height(7);
                }
            });
        }
    }

    private void ComposePhotoWithAccent(ColumnDescriptor col)
    {
        col.Item().AlignCenter().Width(130).Height(140).Layers(layers =>
        {
            layers.Layer()
                .PaddingLeft(20)
                .PaddingTop(6)
                .Width(110)
                .Height(110)
                .Background(DesignCStyles.BlobColor);

            layers.Layer()
                .AlignLeft()
                .AlignBottom()
                .PaddingLeft(4)
                .PaddingBottom(6)
                .Text("\u25CF")
                .FontSize(24)
                .FontColor(DesignCStyles.BlobAccentDark);

            layers.PrimaryLayer().PaddingTop(10).PaddingLeft(5).Width(110).Height(110).Element(container =>
            {
                var framed = container.Border(1).BorderColor("#E5E7EB").Padding(2);
                if (_profileImageBytes is not null)
                {
                    framed.Image(_profileImageBytes).FitArea();
                }
                else
                {
                    framed.Background(DesignCStyles.BlobColor)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(GetInitials())
                        .FontSize(28)
                        .FontColor(DesignCStyles.BlobAccentDark)
                        .Bold();
                }
            });
        });
    }

    private static void ComposeSidebarSectionLabel(ColumnDescriptor col, string icon, string label)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(20).Height(20)
                .Background(DesignCStyles.IconBadgeBg)
                .AlignCenter()
                .AlignMiddle()
                .Text(icon)
                .FontSize(7)
                .FontColor(DesignCStyles.IconBadgeText)
                .Bold();
            r.ConstantItem(6);
            r.RelativeItem().AlignMiddle()
                .Text(label)
                .FontSize(DesignCStyles.SectionTitle)
                .FontColor(DesignCStyles.MainText)
                .Bold()
                .LetterSpacing(0.04f);
        });
        col.Item().Height(7);
    }

    private void ComposeMain(ColumnDescriptor col)
    {
        col.Item().Padding(24).Column(inner =>
        {
            var fullName = $"{_profile.FirstName} {_profile.LastName}".Trim().ToUpperInvariant();
            inner.Item().Text(fullName)
                .FontSize(28f)
                .FontColor(DesignCStyles.MainText)
                .Bold()
                .LetterSpacing(0.02f);

            inner.Item().Height(6);

            if (!string.IsNullOrWhiteSpace(_profile.Headline))
            {
                inner.Item()
                    .Background(DesignCStyles.Cyan)
                    .PaddingHorizontal(6)
                    .PaddingVertical(4)
                    .Text(_profile.Headline.Trim().ToUpperInvariant())
                    .FontSize(DesignCStyles.HeadlineSize)
                    .FontColor(DesignCStyles.MainBg)
                    .Bold()
                    .LetterSpacing(0.05f);
            }

            inner.Item().Height(14);

            if (_workItems.Count > 0)
            {
                ComposeMainSection(inner, "\u2630", "EXPERIENCE");
                foreach (var work in _workItems)
                    ComposeWorkItem(inner, work);
                inner.Item().Height(4);
            }

            if (_educationItems.Count > 0)
            {
                ComposeMainSection(inner, "\u25CE", "EDUCATION");
                foreach (var edu in _educationItems)
                    ComposeEduItem(inner, edu);
                inner.Item().Height(4);
            }

            if (_projects.Count > 0)
            {
                ComposeMainSection(inner, "\u25C8", "PROJECTS");
                foreach (var project in _projects)
                    ComposeProjectItem(inner, project);
            }
        });
    }

    private static void ComposeMainSection(ColumnDescriptor col, string icon, string title)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(20).Height(20)
                .Background(DesignCStyles.IconBadgeBg)
                .AlignCenter()
                .AlignMiddle()
                .Text(icon)
                .FontSize(8)
                .FontColor(DesignCStyles.IconBadgeText)
                .Bold();
            r.ConstantItem(8);
            r.RelativeItem()
                .Text(title)
                .FontSize(DesignCStyles.SectionTitle)
                .FontColor(DesignCStyles.MainText)
                .Bold()
                .LetterSpacing(0.06f);
        });
        col.Item().Height(2).BorderBottom(0.5f).BorderColor(DesignCStyles.SectionLine);
        col.Item().Height(8);
    }

    private static void ComposeWorkItem(ColumnDescriptor col, WorkItemData work)
    {
        var (company, location) = SplitByPipe(work.Company);

        col.Item().Row(r =>
        {
            r.RelativeItem()
                .Text(company)
                .FontSize(DesignCStyles.BodyText)
                .FontColor(DesignCStyles.MainText)
                .Bold();
            r.ConstantItem(70).AlignRight()
                .Text(location ?? string.Empty)
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Row(r =>
        {
            r.RelativeItem()
                .Text(work.Role ?? string.Empty)
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
            r.ConstantItem(90).AlignRight()
                .Text(BuildDateRange(work.StartDate, work.EndDate))
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Height(3);

        var bullets = work.Bullets ?? [];
        if (bullets.Count > 0)
        {
            foreach (var bullet in bullets.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                col.Item().Row(r =>
                {
                    r.ConstantItem(DesignCStyles.BulletIndent)
                        .Text("\u2022")
                        .FontSize(DesignCStyles.BodyText)
                        .FontColor(DesignCStyles.BulletColor);
                    r.RelativeItem()
                        .Text(bullet.Trim())
                        .FontSize(DesignCStyles.BodyText)
                        .FontColor(DesignCStyles.MainText)
                        .LineHeight(1.35f);
                });
                col.Item().Height(2);
            }
        }
        else if (!string.IsNullOrWhiteSpace(work.Description))
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(DesignCStyles.BulletIndent)
                    .Text("\u2022")
                    .FontColor(DesignCStyles.BulletColor);
                r.RelativeItem()
                    .Text(work.Description.Trim())
                    .FontSize(DesignCStyles.BodyText)
                    .LineHeight(1.35f);
            });
        }

        col.Item().Height(DesignCStyles.ItemGap);
    }

    private static void ComposeEduItem(ColumnDescriptor col, EducationItemData edu)
    {
        var (school, location) = SplitByPipe(edu.School);

        col.Item().Row(r =>
        {
            r.RelativeItem()
                .Text(school)
                .FontSize(DesignCStyles.BodyText)
                .Bold();
            r.ConstantItem(70).AlignRight()
                .Text(location ?? string.Empty)
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Row(r =>
        {
            r.RelativeItem()
                .Text(edu.Degree ?? string.Empty)
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
            r.ConstantItem(90).AlignRight()
                .Text(BuildDateRange(edu.StartDate, edu.EndDate))
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Height(DesignCStyles.ItemGap);
    }

    private static void ComposeProjectItem(ColumnDescriptor col, ResumeProjectItem project)
    {
        col.Item().Text(project.Name ?? string.Empty)
            .FontSize(DesignCStyles.BodyText)
            .Bold();

        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(DesignCStyles.BulletIndent)
                    .Text("\u2022")
                    .FontColor(DesignCStyles.BulletColor);
                r.RelativeItem()
                    .Text(project.Description.Trim())
                    .FontSize(DesignCStyles.BodyText)
                    .FontColor(DesignCStyles.MainText)
                    .LineHeight(1.35f);
            });
        }

        var technologies = project.Technologies ?? [];
        if (technologies.Count > 0)
        {
            var tech = technologies.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim());
            col.Item().PaddingLeft(DesignCStyles.BulletIndent)
                .Text(string.Join(" \u00B7 ", tech))
                .FontSize(DesignCStyles.SmallText)
                .FontColor(DesignCStyles.CyanDark);
        }

        col.Item().Height(DesignCStyles.ItemGap);
    }

    private List<(string Name, string? Level)> ResolveLanguages()
    {
        var languageGroup = _skills.FirstOrDefault(s =>
            (s.CategoryName ?? string.Empty).Contains("sprach", StringComparison.OrdinalIgnoreCase));

        var languageItems = languageGroup?.Items ?? [];
        if (languageItems.Count == 0)
            return [];

        return languageItems
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(ParseLanguage)
            .ToList();
    }

    private static (string Name, string? Level) ParseLanguage(string raw)
    {
        var value = raw.Trim();
        var open = value.IndexOf('(');
        var close = value.IndexOf(')');
        if (open > 0 && close > open)
        {
            var name = value[..open].Trim();
            var level = value[(open + 1)..close].Trim();
            return (name, level);
        }

        var parts = value.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var last = parts[^1];
            if (last.Length <= 3 || last.Contains('-'))
                return (string.Join(" ", parts[..^1]), last);
        }

        return (value, null);
    }

    private string GetInitials()
    {
        var first = (_profile.FirstName ?? string.Empty).FirstOrDefault();
        var last = (_profile.LastName ?? string.Empty).FirstOrDefault();
        return $"{first}{last}".ToUpperInvariant();
    }

    private static string GetLanguageDots(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "c2" or "advanced" or "muttersprache" or "native" => "\u25CF\u25CF\u25CF\u25CF",
            "c1" or "upper-intermediate" => "\u25CF\u25CF\u25CF\u25CB",
            "b2" or "intermediate" => "\u25CF\u25CF\u25CB\u25CB",
            "b1" or "lower-intermediate" => "\u25CF\u25CB\u25CB\u25CB",
            _ => "\u25CF\u25CF\u25CB\u25CB"
        };
    }

    private static (string Name, string? Location) SplitByPipe(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, null);

        var parts = raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => (raw.Trim(), null),
            1 => (parts[0], null),
            _ => (parts[0], parts[1])
        };
    }

    private static string BuildDateRange(string startDate, string endDate)
    {
        var normalizedStart = NormalizeDate(startDate);
        var normalizedEnd = NormalizeDate(endDate);

        if (string.IsNullOrWhiteSpace(normalizedStart) && string.IsNullOrWhiteSpace(normalizedEnd))
            return string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEnd))
            return normalizedStart;
        if (string.IsNullOrWhiteSpace(normalizedStart))
            return normalizedEnd;

        return $"{normalizedStart} \u2013 {normalizedEnd}";
    }

    private static string NormalizeDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var value = raw.Trim();
        if (value.Equals("present", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("current", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("heute", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("aktuell", StringComparison.OrdinalIgnoreCase))
        {
            return "Heute";
        }

        var formats = new[] { "MM/yyyy", "M/yyyy", "MM.yyyy", "M.yyyy", "yyyy-MM", "yyyy" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("MM/yyyy", CultureInfo.InvariantCulture);

        return value;
    }
}
