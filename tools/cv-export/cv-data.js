export const cvData = {
  name: "Zouhair Ijaad",
  title: "Softwareentwickler",
  contact: {
    email: "ijd.zouh@outlook.com",
    phone: "+49 (0) 177 227 15 89",
    city: "Hamburg",
    linkedin: "www.linkedin.com/in/zouhair-ijaad"
  },
  summary:
    "IT-affiner Informatik-Absolvent mit Erfahrung in der Analyse, Wartung und Optimierung technischer Systeme. Strukturierte Arbeitsweise im Umgang mit komplexen IT-Umgebungen sowie Fokus auf Fehleranalysen bei Software- und Systemproblemen. Hohe Serviceorientierung und Freude am direkten Anwenderkontakt.",
  experience: [
    {
      company: "SentialNet Personal Networks GmbH",
      location: "Hamburg",
      period: "06/2023 – 07/2024",
      role: "Werkstudent Softwareentwicklung",
      bullets: [
        "Entwicklung einer internen Business-App mit .NET 8 & Blazor – von der Konzeption bis zum produktiven Einsatz im gesamten Team.",
        "Reduzierung der Seitenladezeiten um ~30 % durch Performance-Profiling und Optimierung von API-Aufrufen.",
        "Eigenständige Implementierung von 5+ UI-Modulen inkl. REST-API-Anbindung und Fehlerbehandlung.",
        "Aufbau eines Developer-Wikis als zentrale Wissensbasis für das gesamte Entwicklungsteam."
      ]
    },
    {
      company: "Spielmobil Falkenflitzer",
      location: "Hamburg",
      period: "10/2022 – 09/2024",
      role: "Honorarkraft – Thesisprojekt Vereinsmanagement",
      bullets: [
        "Konzeption und Umsetzung einer Vollstack-Vereins-App (.NET 8, Blazor, EF Core) von Anforderungsanalyse bis Deployment.",
        "Automatisierung von Einsatz- und Schichtplanung für 50+ Ehrenamtliche; manueller Planungsaufwand um ~60 % reduziert.",
        "Implementierung eines rollenbasierten Auth-Systems (JWT/Identity) mit 3 Berechtigungsebenen.",
        "Datenmodellierung und Reporting-Dashboard zur Auswertung von Aktivitäten und Nutzerdaten in Echtzeit."
      ]
    },
    {
      company: "Digi.bo GmbH",
      location: "Hamburg",
      period: "10/2021 – 03/2022",
      role: "Werkstudent Softwareentwicklung & Redaktion",
      bullets: [
        "Weiterentwicklung einer webbasierten Plattform mit React (JSX) & WordPress; Umsetzung neuer Features und Anpassung bestehender Komponenten.",
        "Erstellung und Pflege redaktioneller Inhalte (SEO-optimierte Texte, Landingpages) in enger Abstimmung mit dem Marketing-Team.",
        "Analyse und Behebung von Frontend- & Backend-Bugs; die Fehlerquote in der Produktion wurde messbar gesenkt.",
        "Datenpflege und Qualitätssicherung in verteilten CMS- und Datenbankumgebungen."
      ]
    }
  ],
  skills: [
    {
      category: "Programmiersprachen",
      items: ["C#", ".NET", "Blazor", "MAUI", "HTML", "CSS", "JavaScript"]
    },
    {
      category: "Datenbanken",
      items: ["PostgreSQL", "SQL Server", "MySQL", "Entity Framework Core"]
    },
    {
      category: "Tools & Methoden",
      items: ["REST-Schnittstellen", "Layered Architecture", "Git", "Azure", "Docker"]
    }
  ],
  education: [
    {
      school: "Hochschule Flensburg",
      city: "Flensburg",
      period: "10/2016 – 09/2020",
      degree: "Bachelor of Applied Science – Angewandte Informatik"
    },
    {
      school: "Cadi Ayyad University",
      city: "Essaouira",
      period: "09/2012 – 07/2014",
      degree: "Universitätsdiplom Web-Berufe"
    }
  ],
  projects: [
    {
      name: "Vereins-App Falkenflitzer",
      stack: ".NET 8, Blazor, EF Core, Azure Static Web Apps",
      bullets: [
        "Planungs- und Kommunikationsplattform für Ehrenamtliche mit automatisierter Einsatzplanung.",
        "Azure-Deployment inkl. CI/CD-Pipeline für stabile Releases."
      ]
    },
    {
      name: "Monitoring Dashboard",
      stack: "ASP.NET Core API, React, PostgreSQL, Docker",
      bullets: [
        "Dashboard zur Überwachung von Support-Tickets mit JWT-gesicherter API.",
        "Containerisiertes Setup via Docker Compose für reproduzierbare Umgebungen."
      ]
    }
  ],
  languagesInterests: {
    languages: "Deutsch: C2 | Französisch: C1 | Englisch: B1",
    interests: "Segeln & Wassersport | Street-Fotografie | Ehrenamtliche Jugendarbeit"
  }
};
