# Architecture

cv.studio follows Clean Architecture with strict dependency direction toward the domain core.

## Layer Overview

`Domain`
- Contains business entities (`Resume`, `Snapshot`) and domain-centric state.
- No framework or infrastructure dependency.

`Application`
- Contains use-case services, DTOs, contracts, validation, and mapping.
- Depends on abstractions (`IResumeRepository`, `ISnapshotRepository`, `IApplicationDbContext`) rather than concrete infrastructure.

`Infrastructure`
- Implements persistence and external integrations.
- Includes EF Core DbContext, repository implementations, and PDF/DOCX generators.

`API`
- Exposes the application layer through REST endpoints.
- Handles request validation behavior, middleware, and dependency composition.

`Blazor`
- Server-side UI for creating/editing resumes, managing snapshots, and export actions.
- Communicates with API and keeps UI concerns separate from business logic.

## Dependency Flow

`Blazor` -> `API` -> `Application` -> `Domain`  
`Infrastructure` plugs into `Application` abstractions via dependency injection.

## Cross-Cutting Decisions

- Validation in application services using data annotations.
- Centralized exception handling in API middleware.
- Migration bootstrap on API startup in development environments.
