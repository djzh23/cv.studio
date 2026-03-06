using CvStudio.Application.Contracts;
using CvStudio.Application.DTOs;
using CvStudio.Application.Exceptions;

namespace CvStudio.Application.Templates;

public static class ResumeTemplateCatalog
{
    public const string SoftwareDeveloper = "software-developer";
    public const string ItSupport = "it-support";
    public const string ServiceGeneral = "service-general";

    public static IReadOnlyList<ResumeTemplateDto> List()
    {
        return
        [
            new ResumeTemplateDto
            {
                Key = SoftwareDeveloper,
                DisplayName = "Software Entwickler",
                Description = "Backend/Frontend-orientierter CV mit Projekten und Tech-Stack."
            },
            new ResumeTemplateDto
            {
                Key = ItSupport,
                DisplayName = "IT Supporter",
                Description = "Support-, Infrastruktur- und Incident-Fokus."
            },
            new ResumeTemplateDto
            {
                Key = ServiceGeneral,
                DisplayName = "Service / Gastro / Kommissionierer / Briefzusteller",
                Description = "Allgemeines Service-Profil für operative und kundennahe Tätigkeiten."
            }
        ];
    }

    public static (string Title, ResumeData Data) GetDefaultResume(string templateKey)
    {
        return templateKey switch
        {
            SoftwareDeveloper => ("Softwareentwickler CV", CreateSoftwareDeveloperData()),
            ItSupport => ("IT Support CV", CreateItSupportData()),
            ServiceGeneral => ("Service CV", CreateServiceData()),
            _ => throw new NotFoundException($"Template '{templateKey}' was not found.")
        };
    }

    private static ResumeData CreateSoftwareDeveloperData()
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Zouhair",
                LastName = "Ijaad",
                Headline = "Softwareentwickler",
                Email = "ijd.zouh@outlook.com",
                Phone = "+49 (0) 177 227 15 89",
                Location = "Samlandweg 11, 22415 Hamburg",
                ProfileImageUrl = "https://i.ibb.co/CpTGqYTz/bewerbungsfoto.png",
                Summary = ".NET-Entwickler mit praktischer Erfahrung in Blazor Hybrid, MVVM und REST-API-Anbindung. Vertraut mit C#, Entity Framework Core und Clean Architecture. Schnelle Einarbeitung, strukturierte Arbeitsweise, Teamplayer."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "Digi.bo GmbH | Hamburg",
                    Role = "Werkstudent Softwareentwicklung",
                    StartDate = "10/2021",
                    EndDate = "03/2022",
                    Description = "Mitentwicklung der DIGI:BO-Plattform für digitale Berufsorientierung.",
                    Bullets =
                    [
                        "Weiterentwicklung der WordPress/React-Plattform mit PHP, JSX und Plugin-Anpassungen.",
                        "Formulare und Custom Post Types in bestehende Arbeitsabläufe integriert.",
                        "Redaktionelle Inhalte und Wiki-Dokumentation für das Team erstellt."
                    ]
                },
                new WorkItemData
                {
                    Company = "SentialNet Personal Networks GmbH | Hamburg",
                    Role = "Werkstudent Softwareentwicklung",
                    StartDate = "06/2023",
                    EndDate = "07/2024",
                    Description = "Blazor-Hybrid-App für Android, iOS und Web mitentwickelt.",
                    Bullets =
                    [
                        "UI-Komponenten nach MVVM mit Razor Pages, Commands, Events und Services umgesetzt.",
                        "Caching und asynchrone Verarbeitung zur Ladezeitreduktion eingeführt.",
                        "Datenbankanbindung mit Entity Framework und ORM-Konzepten umgesetzt.",
                        "Ladezeiten um 35 % durch Performance-Optimierung reduziert.",
                        "Stabiler mobiler Zugriff im internen Netzwerk mit hoher Nutzungsakzeptanz erreicht."
                    ]
                },
                new WorkItemData
                {
                    Company = "Spielmobil Falkenflitzer | Hamburg",
                    Role = "Honorarkraft und Thesisprojekt Vereinsmanagement",
                    StartDate = "10/2022",
                    EndDate = "09/2024",
                    Description = "Vereinsmanagement-Anwendung mit .NET MAUI (Frontend) und Laravel (Backend) entwickelt.",
                    Bullets =
                    [
                        "Nutzerverwaltung und Prozessautomatisierung im Vereinskontext umgesetzt.",
                        "REST-APIs mit JSON sowie Datenhaltung mit MySQL und Eloquent implementiert."
                    ]
                },
                new WorkItemData
                {
                    Company = "Eigenprojekte und Weiterbildung | Hamburg",
                    Role = ".NET Backend Entwickler",
                    StartDate = "07/2024",
                    EndDate = "Aktuell",
                    Description = "Eigenprojekte mit .NET, PostgreSQL, Docker und EF Core aufgebaut.",
                    Bullets =
                    [
                        "GitHub-Projekte wie Monitoring Dashboard und Schichtplanung ONP weiterentwickelt.",
                        "Weiterbildung in Clean Architecture und Backend-Strukturierung vertieft."
                    ]
                },
                new WorkItemData
                {
                    Company = "ONP Office National des Peches | Casablanca",
                    Role = "Praktikant Softwareentwicklung",
                    StartDate = "04/2014",
                    EndDate = "07/2014",
                    Description = "Anwendung zur Automatisierung von Schichtplanung und Urlaubsverwaltung entwickelt.",
                    Bullets =
                    [
                        "Reports und technische Dokumentation erstellt.",
                        "Enge Zusammenarbeit mit Fachbereichen im operativen Einsatz."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Hochschule Flensburg | Flensburg",
                    Degree = "Bachelor of Applied Science, Angewandte Informatik",
                    StartDate = "10/2016",
                    EndDate = "09/2020"
                },
                new EducationItemData
                {
                    School = "Cadi Ayyad University | Essaouira",
                    Degree = "Universitätsdiplom, Web-Berufe",
                    StartDate = "09/2012",
                    EndDate = "07/2014"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "Backend-Entwicklung", Items = ["C#", ".NET 8/10", "ASP.NET Core", "REST-APIs", "Entity Framework Core", "Clean Architecture"] },
                new SkillGroupData { CategoryName = "Web- und App-Entwicklung", Items = ["Blazor Hybrid", "MVVM", ".NET MAUI", "Razor Pages", "Laravel"] },
                new SkillGroupData { CategoryName = "Datenbanken", Items = ["PostgreSQL", "MySQL"] },
                new SkillGroupData { CategoryName = "Arbeitsweise und Zusammenarbeit", Items = ["Strukturierte Arbeitsweise", "Schnelle Einarbeitung", "Teamplayer", "Code Reviews", "Swagger"] },
                new SkillGroupData { CategoryName = "Sprachkenntnisse", Items = ["Deutsch (C2)", "Französisch (C1)", "Englisch (B1)"] },
                new SkillGroupData { CategoryName = "Links", Items = ["www.linkedin.com/in/zouhair-ijaad", "https://github.com/djzh23"] }
            ],
            Hobbies = ["Open-Source", "Technologie", "Kontinuierliche Weiterbildung"]
        };
    }

    private static ResumeData CreateItSupportData()
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Zouhair",
                LastName = "Ijaad",
                Headline = "IT Support / Serviceorientierter Mitarbeiter",
                Email = "zouh.ijd@gmail.com",
                Phone = "(+49) 0177 227 15 89",
                Location = "Samlandweg 11, 22415 Hamburg",
                ProfileImageUrl = "https://i.ibb.co/CpTGqYTz/bewerbungsfoto.png",
                Summary = "Engagierter und vielseitiger Mitarbeiter mit Erfahrung in IT-Support, administrativen Aufgaben und Kundenservice. Sicher im Umgang mit Windows, Office 365, digitalen Systemen und IT-Ticketsystemen. Schnelle Einarbeitung in neue Tools und strukturierte Arbeitsweise."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "SentialNet Personal Networks GmbH | Versmold",
                    Role = "Werkstudent und Praktikum",
                    StartDate = "06/2023",
                    EndDate = "07/2024",
                    Bullets =
                    [
                        "Mitarbeit an einer internen Business-App inkl. Grundfunktionen, Datenanbindung und Dokumentation.",
                        "Unterstützung digitaler Prozesse, Testing und strukturierte Teamarbeit.",
                        "Sicherer Umgang mit modernen Tools und IT-Systemen im Tagesbetrieb."
                    ]
                },
                new WorkItemData
                {
                    Company = "Spielmobil Falkenflitzer | Hamburg",
                    Role = "Honorarkraft",
                    StartDate = "10/2022",
                    EndDate = "09/2024",
                    Bullets =
                    [
                        "Unterstützung eines mobilen Teams bei Spiel- und Lernangeboten.",
                        "Organisation sicherer Abläufe, Teamkoordination und Kommunikation im Verein.",
                        "Parallele Entwicklung einer digitalen Vereinslösung zur Prozessoptimierung."
                    ]
                },
                new WorkItemData
                {
                    Company = "BalticBootCenter GmbH | Lübeck",
                    Role = "Saisonarbeitskraft Kundenservice und Vermietung",
                    StartDate = "05/2025",
                    EndDate = "09/2025",
                    Bullets =
                    [
                        "Betreuung und Einweisung von Kunden sowie Sicherstellung von Sicherheitsvorgaben.",
                        "Pflege, Reinigung und Instandhaltung der Boote.",
                        "Zuverlässige, serviceorientierte Kommunikation in Deutsch und Englisch."
                    ]
                },
                new WorkItemData
                {
                    Company = "Deutsche Post AG | Flensburg",
                    Role = "Werkstudent Brief- und Zustelldienst",
                    StartDate = "10/2019",
                    EndDate = "06/2023",
                    Bullets =
                    [
                        "Sortierung und Zustellung von Briefsendungen, Paketen und Einschreiben.",
                        "Eigenständige Tourplanung sowie Bargeldabwicklung.",
                        "Zuverlässige und genaue Arbeitsweise unter Zeitdruck."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Hochschule Flensburg | Flensburg",
                    Degree = "Bachelor of Applied Science, Angewandte Informatik",
                    StartDate = "10/2016",
                    EndDate = "09/2020"
                },
                new EducationItemData
                {
                    School = "Cadi Ayyad University",
                    Degree = "Universitätsdiplom, Web-Berufe",
                    StartDate = "09/2012",
                    EndDate = "07/2014"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "IT-Support", Items = ["Windows", "Office 365", "IT-Ticketsysteme", "Troubleshooting", "Serviceorientierte Kommunikation"] },
                new SkillGroupData { CategoryName = "Digitale Prozesse", Items = ["Testing", "Dokumentation", "Strukturierte Teamarbeit"] },
                new SkillGroupData { CategoryName = "Sprachkenntnisse", Items = ["Deutsch (C1)", "Französisch (C1)", "Englisch (B1-B2)"] }
            ],
            Hobbies = ["Kultur und Sprachen", "Musik", "Fußball", "Wandern", "Technik und digitale Tools"]
        };
    }

    private static ResumeData CreateServiceData()
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Max",
                LastName = "Mustermann",
                Headline = "Servicekraft / Kommissionierer / Briefzusteller",
                Email = "max@example.com",
                Phone = "+49 170 0000000",
                Location = "Hamburg, Deutschland",
                ProfileImageUrl = "https://i.ibb.co/CpTGqYTz/bewerbungsfoto.png",
                Summary = "Belastbarer und zuverlässiger Mitarbeiter mit Erfahrung in Service, Logistik und kundennahem Arbeiten."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "City Service GmbH | Hamburg",
                    Role = "Servicekraft",
                    StartDate = "04/2021",
                    EndDate = "Heute",
                    Bullets =
                    [
                        "Freundliche Betreuung von Gästen und effiziente Auftragsabwicklung.",
                        "Sicherstellung von Qualitäts- und Hygienestandards im Tagesbetrieb.",
                        "Verlässliche Zusammenarbeit in Schicht- und Stoßzeiten."
                    ]
                },
                new WorkItemData
                {
                    Company = "Logistik Nord | Hamburg",
                    Role = "Kommissionierer / Zusteller",
                    StartDate = "09/2019",
                    EndDate = "03/2021",
                    Bullets =
                    [
                        "Kommissionierung und termingerechte Bereitstellung von Sendungen.",
                        "Sorgfältige Erfassung und Kontrolle von Warenbewegungen.",
                        "Zuverlässige Auslieferung und kundenorientierte Übergabe."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Berufsbildende Schule | Hamburg",
                    Degree = "Allgemeiner Schulabschluss",
                    StartDate = "08/2015",
                    EndDate = "07/2018"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "Service", Items = ["Kundenbetreuung", "Kassensysteme", "Teamarbeit"] },
                new SkillGroupData { CategoryName = "Logistik", Items = ["Kommissionierung", "Warenkontrolle", "Tourenvorbereitung"] },
                new SkillGroupData { CategoryName = "Arbeitsweise", Items = ["Zuverlässigkeit", "Belastbarkeit", "Pünktlichkeit"] }
            ],
            Hobbies = ["Kochen", "Sport"]
        };
    }
}
