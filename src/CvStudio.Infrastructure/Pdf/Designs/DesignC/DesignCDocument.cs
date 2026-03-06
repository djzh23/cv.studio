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

    public DesignCDocument(ResumeData data, byte[]? profileImageBytes)
    {
        _data = data;
        _profileImageBytes = profileImageBytes;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(x => x
                .FontFamily("Arial")
                .FontSize(DesignCStyles.BodyText)
                .FontColor(DesignCStyles.MainText));

            page.Content().Row(row =>
            {
                row.ConstantItem(168).Background(DesignCStyles.SidebarBg)
                    .Padding(20)
                    .Column(ComposeSidebar);

                // ATS-REGEL 7: Weißer Hintergrund für Haupttext.
                row.RelativeItem().Background("#FFFFFF")
                    .Padding(28)
                    .Column(ComposeMain);
            });
        });
    }

    private void ComposeSidebar(ColumnDescriptor col)
    {
        // ATS-REGEL 1: Kein Text in Grafiken (Foto ohne Text-Overlay).
        if (_profileImageBytes is not null)
        {
            col.Item().AlignCenter().Width(90).Height(90).Element(c =>
            {
                c.Border(1).BorderColor(DesignCStyles.Accent).Padding(2).Image(_profileImageBytes).FitArea();
            });
            col.Item().Height(16);
        }

        ComposeSidebarSection(col, "KONTAKT");
        if (!string.IsNullOrWhiteSpace(_data.Profile.Phone))
            ComposeSidebarItem(col, _data.Profile.Phone);
        if (!string.IsNullOrWhiteSpace(_data.Profile.Email))
            ComposeSidebarItem(col, _data.Profile.Email);
        if (!string.IsNullOrWhiteSpace(_data.Profile.Location))
            ComposeSidebarItem(col, _data.Profile.Location);

        var languageGroup = _data.Skills.FirstOrDefault(s =>
            s.CategoryName.Contains("sprach", StringComparison.OrdinalIgnoreCase));
        if (languageGroup is not null && languageGroup.Items.Count > 0)
        {
            col.Item().Height(DesignCStyles.SectionGap);
            // ATS-REGEL 2: Sprachen als Plain-Text (keine Punkte-Grafik).
            ComposeSidebarSection(col, "SPRACHEN");
            foreach (var language in languageGroup.Items.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                col.Item().Text(language.Trim())
                    .FontSize(DesignCStyles.SidebarBody)
                    .FontColor(DesignCStyles.SidebarText);
                col.Item().Height(3);
            }
        }

        var skillGroups = _data.Skills
            .Where(s => !s.CategoryName.Contains("sprach", StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Items.Count > 0)
            .ToList();

        if (skillGroups.Count > 0)
        {
            col.Item().Height(DesignCStyles.SectionGap);
            // ATS-REGEL 3: Skills nach Kategorien gruppiert.
            ComposeSidebarSection(col, "SKILLS");
            foreach (var group in skillGroups)
            {
                col.Item().Text(group.CategoryName.Trim())
                    .FontSize(DesignCStyles.SmallText)
                    .FontColor(DesignCStyles.Accent)
                    .Bold();
                col.Item().Height(2);

                col.Item().Text(string.Join(" · ", group.Items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())))
                    .FontSize(DesignCStyles.SidebarBody)
                    .FontColor(DesignCStyles.SidebarText)
                    .LineHeight(1.3f);
                col.Item().Height(6);
            }
        }
    }

    private static void ComposeSidebarSection(ColumnDescriptor col, string title)
    {
        col.Item().BorderBottom(1).BorderColor(DesignCStyles.Accent).PaddingBottom(3)
            .Text(title)
            .FontSize(DesignCStyles.SidebarTitle)
            .FontColor(DesignCStyles.Accent)
            .Bold()
            .LetterSpacing(0.08f);
        col.Item().Height(6);
    }

    private static void ComposeSidebarItem(ColumnDescriptor col, string text)
    {
        col.Item().Text(text.Trim())
            .FontSize(DesignCStyles.SmallText)
            .FontColor(DesignCStyles.SidebarText)
            .LineHeight(1.3f);
        col.Item().Height(4);
    }

    private void ComposeMain(ColumnDescriptor col)
    {
        var fullName = $"{_data.Profile.FirstName} {_data.Profile.LastName}".Trim();
        col.Item().Text(fullName)
            .FontSize(DesignCStyles.NameSize)
            .FontColor(DesignCStyles.MainText)
            .Bold();

        col.Item().Height(6);

        if (!string.IsNullOrWhiteSpace(_data.Profile.Headline))
        {
            col.Item().Background(DesignCStyles.HeadlineBg).Padding(5, 3)
                .Text(_data.Profile.Headline.Trim())
                .FontSize(DesignCStyles.HeadlineSize)
                .FontColor(DesignCStyles.HeadlineText)
                .Bold();
        }

        col.Item().Height(12);

        if (!string.IsNullOrWhiteSpace(_data.Profile.Summary))
        {
            col.Item().Text(_data.Profile.Summary.Trim())
                .FontSize(DesignCStyles.BodyText)
                .FontColor(DesignCStyles.MainText)
                .LineHeight(1.5f);
            col.Item().Height(DesignCStyles.SectionGap);
        }

        if (_data.WorkItems.Count > 0)
        {
            // ATS-REGEL 4: Section-Titel als echter Text (keine Grafik).
            ComposeMainSection(col, "BERUFSERFAHRUNG");
            foreach (var work in _data.WorkItems)
            {
                ComposeWorkItem(col, work);
            }
        }

        if (_data.EducationItems.Count > 0)
        {
            ComposeMainSection(col, "AUSBILDUNG");
            foreach (var edu in _data.EducationItems)
            {
                ComposeEducationItem(col, edu);
            }
        }
    }

    private static void ComposeMainSection(ColumnDescriptor col, string title)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem()
                .BorderBottom(1.5f)
                .BorderColor("#111827")
                .PaddingBottom(3)
                .Text(title)
                .FontSize(DesignCStyles.SectionTitle)
                .Bold()
                .LetterSpacing(0.06f);
        });
        col.Item().Height(8);
    }

    private static void ComposeWorkItem(ColumnDescriptor col, WorkItemData work)
    {
        var (company, location) = SplitByPipe(work.Company);

        // ATS-REGEL 5: Firma links, Datum rechts als getrennte Textspalten.
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(company).FontSize(DesignCStyles.BodyText).Bold();
            r.ConstantItem(80).AlignRight().Text(location ?? string.Empty)
                .FontSize(DesignCStyles.SmallText).FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Row(r =>
        {
            r.RelativeItem().Text(work.Role).FontSize(DesignCStyles.BodyText).FontColor(DesignCStyles.MainMuted);
            r.ConstantItem(100).AlignRight().Text(BuildDateRange(work.StartDate, work.EndDate))
                .FontSize(DesignCStyles.SmallText).FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Height(3);

        if (work.Bullets.Count > 0)
        {
            foreach (var bullet in work.Bullets.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                col.Item().Row(r =>
                {
                    // ATS-REGEL 6: Bullet als echtes Zeichen.
                    r.ConstantItem(DesignCStyles.BulletIndent).Text("•")
                        .FontSize(DesignCStyles.BodyText).FontColor(DesignCStyles.Accent);
                    r.RelativeItem().Text(bullet.Trim()).FontSize(DesignCStyles.BodyText).LineHeight(1.4f);
                });
                col.Item().Height(2);
            }
        }
        else if (!string.IsNullOrWhiteSpace(work.Description))
        {
            col.Item().Text(work.Description.Trim()).FontSize(DesignCStyles.BodyText).LineHeight(1.4f);
        }

        col.Item().Height(DesignCStyles.ItemGap);
    }

    private static void ComposeEducationItem(ColumnDescriptor col, EducationItemData edu)
    {
        var (school, location) = SplitByPipe(edu.School);
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(school).FontSize(DesignCStyles.BodyText).Bold();
            r.ConstantItem(80).AlignRight().Text(location ?? string.Empty)
                .FontSize(DesignCStyles.SmallText).FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Row(r =>
        {
            r.RelativeItem().Text(edu.Degree).FontSize(DesignCStyles.BodyText).FontColor(DesignCStyles.MainMuted);
            r.ConstantItem(100).AlignRight().Text(BuildDateRange(edu.StartDate, edu.EndDate))
                .FontSize(DesignCStyles.SmallText).FontColor(DesignCStyles.MainMuted);
        });

        col.Item().Height(DesignCStyles.ItemGap);
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

        return $"{normalizedStart} – {normalizedEnd}";
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
        {
            return dt.ToString("MM/yyyy", CultureInfo.InvariantCulture);
        }

        return value;
    }
}
