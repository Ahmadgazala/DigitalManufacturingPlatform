# AGENTS.md

## Build, tests, and lint gates

Warnings are treated as errors in every project (`TreatWarningsAsErrors` + `AnalysisLevel=latest`).
A clean build and passing tests are **required** before committing or deploying.

```sh
# full build (solution with web + tests) — must be 0 warnings / 0 errors
dotnet build DMP.slnx -c Release

# run the xUnit test suite
dotnet test DMP.slnx -c Release

# verify the production publish pipeline (what Render's Dockerfile runs)
dotnet publish src/DMP.Web/DMP.Web.csproj -c Release -o /tmp/publish-check
```

CI (`.github/workflows/ci.yml`) runs restore → build → test → publish on every push to `main`
and on PRs. Render auto-deploys `main`; this CI is the pre-ship quality gate.

## Structure

- `src/DMP.Web` — ASP.NET Core MVC app (net10.0, EF Core + SQLite local / Npgsql on Render).
- `tests/DMP.Web.Tests` — xUnit unit + in-memory EF tests (CartService, FileService, Product/Order logic).
- `DMP.slnx` — solution linking web + test projects.

## Notes

- Local dev DB is SQLite (`DMP_Dev.db`); production uses Postgres via Render's `DATABASE_URL`.
- DB schema on the existing prod DB is bootstrapped by raw SQL in `Program.cs` (EnsureCreated no-ops there).
