# cv.studio

> Personal CV & Application Manager.
> Built with Blazor Server, .NET 8, and Clean Architecture.

![.NET](https://img.shields.io/badge/.NET-8-purple)
![Blazor](https://img.shields.io/badge/Blazor-Server-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![Docker](https://img.shields.io/badge/Docker-ready-blue)
![CI](https://github.com/djzh23/cv.studio/actions/workflows/ci.yml/badge.svg)

## Features
- CV templates (Software, IT Support, Service)
- Auto-save, snapshot variants, live editing workflow
- Export as PDF (QuestPDF) and DOCX (OpenXML)

## Stack
Blazor Server · ASP.NET Core Web API · .NET 8 · PostgreSQL · EF Core · Docker

## Architecture
Domain -> Application -> Infrastructure -> API -> Blazor

## Local Start
```bash
docker-compose up --build
dotnet build cv.studio.sln
dotnet test cv.studio.sln
```

## Live Demo
https://cv-studio.railway.app
