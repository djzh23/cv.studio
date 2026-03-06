using FluentAssertions;
using CvStudio.Application.Contracts;
using CvStudio.Application.Services;

namespace CvStudio.UnitTests.Services;

public sealed class AtsScoreServiceTests
{
    private readonly AtsScoreService _sut = new();

    [Theory]
    [InlineData("Wir suchen C# .NET Entwickler mit Blazor", JobCategory.SoftwareEntwickler)]
    [InlineData("IT-Support First Level Helpdesk Ticketsystem", JobCategory.ItSupport)]
    [InlineData("Servicekraft Küche Zustellung Kommissionierung", JobCategory.Allgemein)]
    public void DetectCategory_ReturnsCorrectCategory(string jobDesc, JobCategory expected)
    {
        // Arrange
        var resume = BuildResume();

        // Act
        var result = _sut.Calculate(resume, jobDesc, JobCategory.Auto);

        // Assert
        result.DetectedCategory.Should().Be(expected);
    }

    [Fact]
    public void Calculate_SoftwareCategory_UsesCodeVerbs()
    {
        // Arrange
        var resume = BuildResume(summary: "Blazor App implementiert und deployed");

        // Act
        var result = _sut.Calculate(resume, "Entwickler gesucht", JobCategory.SoftwareEntwickler);

        // Assert
        result.LanguageScore.Should().BeGreaterOrEqualTo(5);
    }

    [Fact]
    public void Calculate_AllgemeinCategory_RecognizesLogistikMetrics()
    {
        // Arrange
        var resume = BuildResume(summary: "Täglich 200 Pakete sortiert und zugestellt");

        // Act
        var result = _sut.Calculate(resume, "Zustellung Logistik", JobCategory.Allgemein);

        // Assert
        result.LanguageScore.Should().BeGreaterOrEqualTo(10);
    }

    private static ResumeData BuildResume(string summary = "Erfahrung im IT-Umfeld")
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Max",
                LastName = "Mustermann",
                Headline = "Bewerber",
                Email = "max@example.com",
                Phone = "+49 170 0000000",
                Summary = summary,
                Location = "Hamburg"
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "Firma A",
                    Role = "Mitarbeiter",
                    StartDate = "01/2022",
                    EndDate = "12/2023",
                    Description = "Aufgaben umgesetzt",
                    Bullets = ["Punkt 1"]
                }
            ],
            Skills =
            [
                new SkillGroupData
                {
                    CategoryName = "Technik",
                    Items = ["C#", "dotnet"]
                }
            ]
        };
    }
}