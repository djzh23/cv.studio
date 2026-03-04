# Workflow

Branch strategy:

- `main`: protected, merge via PR only
- `develop`: active integration branch
- `feature/*`: new features
- `fix/*`: bug fixes
- `refactor/*`: restructuring

Rules:

1. Branch from `develop`
2. Keep commits small and scoped
3. Open PR to `develop`
4. Merge after review and passing CI
5. Rebase/sync before new work
