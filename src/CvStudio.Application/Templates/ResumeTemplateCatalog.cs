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
                FirstName = "Max",
                LastName = "Mustermann",
                Headline = "Softwareentwickler",
                Email = "max.mustermann@example.de",
                Phone = "+49 (0) 170 000 00 00",
                Location = "Musterstraße 1, 12345 Musterstadt",
                ProfileImageUrl = "",
                Summary = "Engagierte Fachkraft mit Erfahrung in der Projektarbeit, Prozessoptimierung und Teamkollaboration. Strukturierte und lösungsorientierte Arbeitsweise."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "Musterfirma GmbH | Musterstadt",
                    Role = "Softwareentwickler",
                    StartDate = "01/2022",
                    EndDate = "Heute",
                    Description = "Entwicklung und Umsetzung von Projekten im Team.",
                    Bullets =
                    [
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielunternehmen AG | Musterstadt",
                    Role = "Werkstudent Softwareentwicklung",
                    StartDate = "03/2019",
                    EndDate = "12/2021",
                    Description = "Analyse und Optimierung bestehender Prozesse.",
                    Bullets =
                    [
                        "Planung und Durchführung von Workshops und Präsentationen.",
                        "Erstellung von Dokumentationen und Reports.",
                        "Zusammenarbeit mit internen und externen Stakeholdern."
                    ]
                },
                new WorkItemData
                {
                    Company = "Muster GmbH | Musterstadt",
                    Role = "Praktikant Softwareentwicklung",
                    StartDate = "06/2017",
                    EndDate = "02/2019",
                    Description = "Entwicklung und Umsetzung von Projekten im Team.",
                    Bullets =
                    [
                        "Erstellung von Dokumentationen und Reports.",
                        "Zusammenarbeit mit internen und externen Stakeholdern."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielbetrieb e.K. | Musterstadt",
                    Role = "Werkstudent",
                    StartDate = "10/2015",
                    EndDate = "05/2017",
                    Description = "Zusammenarbeit mit internen und externen Stakeholdern.",
                    Bullets =
                    [
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Musterbetrieb GmbH | Musterstadt",
                    Role = "Praktikant",
                    StartDate = "04/2014",
                    EndDate = "07/2014",
                    Description = "Planung und Durchführung von Workshops und Präsentationen.",
                    Bullets =
                    [
                        "Erstellung von Dokumentationen und Reports.",
                        "Zusammenarbeit mit internen und externen Stakeholdern."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Muster-Universität | Musterstadt",
                    Degree = "Bachelor of Science, Musterstudiengang",
                    StartDate = "10/2016",
                    EndDate = "09/2020"
                },
                new EducationItemData
                {
                    School = "Beispiel-Gymnasium | Musterstadt",
                    Degree = "Allgemeine Hochschulreife",
                    StartDate = "09/2008",
                    EndDate = "07/2016"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "Softwareentwicklung", Items = ["Programmierung", "Software-Design", "Testing", "Versionskontrolle"] },
                new SkillGroupData { CategoryName = "Web & App", Items = ["Frontend-Entwicklung", "Backend-Entwicklung", "API-Integration"] },
                new SkillGroupData { CategoryName = "Datenbanken", Items = ["Relationale Datenbanken", "Datenbankanbindung"] },
                new SkillGroupData { CategoryName = "Arbeitsweise", Items = ["Strukturierte Arbeitsweise", "Schnelle Einarbeitung", "Teamarbeit", "Code Reviews"] },
                new SkillGroupData { CategoryName = "Sprachkenntnisse", Items = ["Deutsch (Muttersprache)", "Englisch (B2)"] },
                new SkillGroupData { CategoryName = "Links", Items = ["linkedin.com/in/max-mustermann", "github.com/max-mustermann"] }
            ],
            Hobbies = ["Technologie", "Weiterbildung", "Open-Source"]
        };
    }

    private static ResumeData CreateItSupportData()
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Max",
                LastName = "Mustermann",
                Headline = "IT Support / Serviceorientierter Mitarbeiter",
                Email = "max.mustermann@example.de",
                Phone = "+49 (0) 170 000 00 00",
                Location = "Musterstraße 1, 12345 Musterstadt",
                ProfileImageUrl = "",
                Summary = "Engagierte Fachkraft mit Erfahrung in der Projektarbeit, Prozessoptimierung und Teamkollaboration. Strukturierte und lösungsorientierte Arbeitsweise."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "Musterfirma GmbH | Musterstadt",
                    Role = "IT-Supporter",
                    StartDate = "01/2022",
                    EndDate = "Heute",
                    Bullets =
                    [
                        "Entwicklung und Umsetzung von Projekten im Team.",
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielunternehmen AG | Musterstadt",
                    Role = "Werkstudent IT-Support",
                    StartDate = "03/2019",
                    EndDate = "12/2021",
                    Bullets =
                    [
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Planung und Durchführung von Workshops und Präsentationen.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Muster GmbH | Musterstadt",
                    Role = "Werkstudent",
                    StartDate = "06/2017",
                    EndDate = "02/2019",
                    Bullets =
                    [
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielbetrieb e.K. | Musterstadt",
                    Role = "Aushilfe",
                    StartDate = "10/2015",
                    EndDate = "05/2017",
                    Bullets =
                    [
                        "Planung und Durchführung von Workshops und Präsentationen.",
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Zuverlässige und strukturierte Arbeitsweise."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Muster-Universität | Musterstadt",
                    Degree = "Bachelor of Applied Science, Musterstudiengang",
                    StartDate = "10/2016",
                    EndDate = "09/2020"
                },
                new EducationItemData
                {
                    School = "Beispiel-Gymnasium | Musterstadt",
                    Degree = "Allgemeine Hochschulreife",
                    StartDate = "09/2008",
                    EndDate = "07/2016"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "IT-Support", Items = ["Windows", "Office 365", "IT-Ticketsysteme", "Troubleshooting", "Serviceorientierte Kommunikation"] },
                new SkillGroupData { CategoryName = "Digitale Prozesse", Items = ["Testing", "Dokumentation", "Strukturierte Teamarbeit"] },
                new SkillGroupData { CategoryName = "Sprachkenntnisse", Items = ["Deutsch (Muttersprache)", "Englisch (B2)"] }
            ],
            Hobbies = ["Technik", "Digitale Tools", "Sport"]
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
                Headline = "Servicekraft | Kundenbetreuung & Logistik",
                Email = "max.mustermann@example.de",
                Phone = "+49 (0) 170 000 00 00",
                Location = "Musterstraße 1, 12345 Musterstadt",
                ProfileImageUrl = "",
                WorkPermit = "Unbefristete Arbeitserlaubnis",
                Summary = "Engagierte Fachkraft mit Erfahrung in der Projektarbeit, Prozessoptimierung und Teamkollaboration. Strukturierte und lösungsorientierte Arbeitsweise."
            },
            WorkItems =
            [
                new WorkItemData
                {
                    Company = "Musterfirma GmbH | Musterstadt",
                    Role = "Kundenservice & Servicekraft",
                    StartDate = "01/2022",
                    EndDate = "Heute",
                    Bullets =
                    [
                        "Entwicklung und Umsetzung von Projekten im Team.",
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Planung und Durchführung von Workshops und Präsentationen."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielunternehmen AG | Musterstadt",
                    Role = "Servicekraft",
                    StartDate = "03/2019",
                    EndDate = "12/2021",
                    Bullets =
                    [
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Erstellung von Dokumentationen und Reports.",
                        "Zusammenarbeit mit internen und externen Stakeholdern."
                    ]
                },
                new WorkItemData
                {
                    Company = "Muster GmbH | Musterstadt",
                    Role = "Aushilfe & Werkstudent",
                    StartDate = "06/2017",
                    EndDate = "02/2019",
                    Bullets =
                    [
                        "Planung und Durchführung von Workshops und Präsentationen.",
                        "Zuverlässige und strukturierte Arbeitsweise.",
                        "Erstellung von Dokumentationen und Reports."
                    ]
                },
                new WorkItemData
                {
                    Company = "Beispielbetrieb e.K. | Musterstadt",
                    Role = "Aushilfe",
                    StartDate = "10/2015",
                    EndDate = "05/2017",
                    Bullets =
                    [
                        "Analyse und Optimierung bestehender Prozesse.",
                        "Zusammenarbeit mit internen und externen Stakeholdern.",
                        "Zuverlässige Arbeitsweise unter Zeitdruck."
                    ]
                }
            ],
            EducationItems =
            [
                new EducationItemData
                {
                    School = "Muster-Universität | Musterstadt",
                    Degree = "Bachelor of Science, Musterstudiengang",
                    StartDate = "10/2016",
                    EndDate = "09/2020"
                },
                new EducationItemData
                {
                    School = "Beispiel-Gymnasium | Musterstadt",
                    Degree = "Allgemeine Hochschulreife",
                    StartDate = "09/2008",
                    EndDate = "07/2016"
                }
            ],
            Skills =
            [
                new SkillGroupData { CategoryName = "Kundenservice", Items = ["Kundenbetreuung", "Beratung", "Kassensysteme", "Serviceorientierung"] },
                new SkillGroupData { CategoryName = "Logistik", Items = ["Kommissionierung", "Warenkontrolle", "Lagerverwaltung", "Tourenplanung"] },
                new SkillGroupData { CategoryName = "IT & Tools", Items = ["Windows", "Office 365", "Outlook"] },
                new SkillGroupData { CategoryName = "Arbeitsweise", Items = ["Zuverlässigkeit", "Belastbarkeit", "Pünktlichkeit", "Teamfähigkeit"] },
                new SkillGroupData { CategoryName = "Sprachkenntnisse", Items = ["Deutsch (Muttersprache)", "Englisch (B1)"] }
            ],
            Hobbies = []
        };
    }
}
