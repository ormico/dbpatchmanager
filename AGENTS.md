# Agent Instructions

## Project Overview
.NET 8 database patch management CLI application with GitHub Actions CI/CD and GitVersion automated versioning.

## Commit Messages
Use [Conventional Commits](https://www.conventionalcommits.org/): `<type>(<scope>): <description>`

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `ci`, `chore`

Examples:
```
feat(cli): add init command for database setup
fix(logic): handle null connection string gracefully
test(logic): add unit tests for BuildConfigurationWriter
```

## Versioning
- **GitVersion** (Mainline mode) manages all versions automatically — never manually edit version numbers
- `feature/*` branches → alpha pre-release (e.g., `2.2.3-alpha.5`)
- `main` branch → stable releases (e.g., `2.2.3`)
- Release by tagging main: `git tag v2.2.3`
- Config in `GitVersion.yml` — do not modify without explicit request

## Coding Standards

### C# / .NET 8
- Nullable reference types enabled
- Use `var` for obvious types, explicit otherwise
- XML documentation comments on public APIs
- Follow Microsoft C# conventions

### Testing (xUnit)
- Naming: `[ClassUnderTest]Tests`, `MethodName_Scenario_ExpectedResult`
- Maintain minimum 80% code coverage
- Arrange-act-assert structure

### PowerShell
- Approved verbs only (Get-, Set-, New-, etc.)
- Comment-based help, parameter validation (`[Parameter(Mandatory)]`)
- Informative output (`Write-Host`, `Write-Warning`), try-catch error handling

## CI/CD (GitHub Actions)
- Kebab-case workflow filenames: `pr-build.yml`, `feature-build.yml`
- **Always** use `fetch-depth: 0` for GitVersion
- Use GitVersion outputs (`semVer`, `majorMinorPatch`), never hardcoded versions
- Cache NuGet packages, use secrets for sensitive data

## File Structure
```
/
├── .github/workflows/    # GitHub Actions
├── docs/                 # Documentation
├── scripts/              # Automation scripts
├── src/                  # Source code (Ormico.DbPatchManager.sln)
├── GitVersion.yml
└── README.md
```

## Anti-Patterns
- No manual version numbers or `version.json`
- No hardcoded secrets, credentials, or API keys
- No temporary/summary/completion documents — update existing docs instead
- No `Thread.Sleep()` (use proper async patterns)
- No commented-out tests
- Always use feature branches and PRs, never commit directly to main
