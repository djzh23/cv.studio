# Workflow

## Branching Strategy

- `main`: release branch, protected, merge via pull request only
- `develop`: active integration branch
- `feature/*`: new functionality
- `fix/*`: bug fixes
- `refactor/*`: non-functional structural improvements

## Standard Flow

1. Sync local `develop`
2. Create a scoped branch from `develop`
3. Commit in small, reviewable increments with conventional commit messages
4. Open PR against `develop` with clear technical context and verification notes
5. Merge only after CI is green
6. Start next task only after previous PR is merged

## Pull Request Rules

- PR descriptions in English for portfolio visibility
- Include what changed, why it changed, and how it was validated
- Keep one concern per PR to maintain clean history
- Avoid direct pushes to `main`

## Local Verification Checklist

- `dotnet build cv.studio.sln`
- `dotnet test cv.studio.sln`
- Optional smoke run for API and Blazor before PR
