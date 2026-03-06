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
        result.LanguageScore.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void Calculate_AllgemeinCategory_RecognizesLogistikMetrics()
    {
        // Arrange
        var resume = BuildResume(summary: "Täglich 200 Pakete sortiert und zugestellt");

        // Act
        var result = _sut.Calculate(resume, "Zustellung Logistik", JobCategory.Allgemein);

        // Assert
        result.LanguageScore.Should().BeGreaterOrEqualTo(2);
        result.EvidenceScore.Should().BeGreaterOrEqualTo(7);
    }

    [Fact]
    public void Calculate_MissingMustHaves_PopulatesMissingMustHaveKeywords()
    {
        // Arrange
        var resume = BuildResume(summary: "Erfahrung in Kommunikation und Teamarbeit");

        // Act
        var result = _sut.Calculate(resume, "IT-Support mit Active Directory, Office365 und Ticketsystem", JobCategory.ItSupport);

        // Assert
        result.MissingMustHaveKeywords.Should().NotBeEmpty();
        result.HardRequirementsScore.Should().BeLessThan(30);
    }

    [Fact]
    public void Calculate_IncludesProjectsInEvidence()
    {
        // Arrange
        var resume = BuildResume();
        resume.Projects.Add(new ResumeProjectItem
        {
            Name = "Portfolio Plattform",
            Description = "REST API und Blazor Frontend umgesetzt",
            Technologies = ["dotnet", "blazor", "postgresql"]
        });

        // Act
        var result = _sut.Calculate(resume, "Softwareentwickler C# .NET Blazor REST API", JobCategory.SoftwareEntwickler);

        // Assert
        result.EvidenceScore.Should().BeGreaterThan(0);
        result.Score.Should().BeInRange(0, 100);
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
