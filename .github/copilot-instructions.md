# GitHub Copilot Instructions

## Project Overview
This is a .NET 8 web application template with comprehensive GitHub Actions CI/CD pipeline automation. The project demonstrates enterprise-grade DevOps practices including automated versioning with GitVersion, testing, Docker containerization, and multi-branch workflow strategies.

## 🚫 Documentation Anti-Patterns

### NEVER Create These Types of Files
- ❌ **Summary documents** (e.g., "CONVERSION-SUMMARY.md", "MIGRATION-SUMMARY.md")
- ❌ **"Complete" or "Done" documents** (e.g., "README-COMPLETE.md", "SETUP-COMPLETE.md")
- ❌ **Temporary documentation** that duplicates existing content
- ❌ **"Updates" or "Changes" documents** (e.g., "DOCUMENTATION-UPDATES.md")
- ❌ **Redundant guides** when information already exists in proper docs

### Why These Are Bad
- Creates clutter in the repository
- Adds maintenance burden (must update multiple places)
- Confuses users about which document is authoritative
- No lasting value after initial work is complete
- Makes repository look unprofessional

### Instead, Update Existing Documents
- ✅ Update `README.md` for high-level overview
- ✅ Update relevant docs in `/docs` folder for user guides
- ✅ Keep GitVersion references in `/docs` folder

## 📋 Documentation Standards

### Acceptable Documentation Types
1. **README.md** - Main entry point, high-level overview
2. **User guides** - In `/docs` for end-user functionality
3. **Developer guides** - In `/docs` for contributors
4. **Quick references** - In `/docs` for daily use reference
5. **Test walkthroughs** - In `/docs` for testing procedures

### When to Create New Documentation
- ✅ **New feature** needs user guide
- ✅ **Complex system** needs technical reference that will be used repeatedly
- ✅ **Testing procedure** is complex enough to warrant step-by-step guide
- ✅ **API or interface** needs reference documentation

### When to Update Existing Documentation
- ✅ **Feature changes** - Update relevant existing docs
- ✅ **Process changes** - Update workflow or CI/CD docs
- ✅ **Configuration changes** - Update setup guides
- ✅ **Anything that fits existing document scope**

## Versioning Strategy

### Use GitVersion for ALL Version Management
- **GitVersion** automatically calculates versions (Mainline mode)
- **NEVER** manually update version numbers in files
- **NEVER** suggest creating or updating `version.json` (deprecated)

### Commit Message Keywords
Use when explicit version control is needed:
```bash
+semver: major    # Breaking changes (1.x.x → 2.0.0)
+semver: minor    # New features (x.1.x → x.2.0)
+semver: patch    # Bug fixes (x.x.1 → x.x.2)
+semver: none     # No version bump (docs, tests, refactoring)
```

### Branch Naming Affects Versioning
- `feature/*` → alpha pre-release tags (e.g., `2.2.3-alpha.5`)
- `main` → stable versions (e.g., `2.2.3`)

### Release Process
- **Pure GitHub Flow** - All work happens in feature branches
- Merge to main when ready for production
- Create releases by tagging main: `git tag v2.2.3`
- No release branches needed - simplified workflow

### GitVersion Configuration
- Configuration file: `GitVersion.yml` (repository root)
- Never suggest modifying unless explicitly requested
- Mainline mode is intentional - don't suggest changing to other modes

## Coding Standards

### .NET/C# Conventions
- Target framework: **.NET 8.0** (always use this version)
- Use **minimal APIs** for web projects
- Follow **Microsoft C# coding conventions**
- Enable **nullable reference types**
- Use `var` for obvious types, explicit types otherwise
- **XML documentation comments** for public APIs

### Testing Standards
- Use **xUnit** for unit tests
- Test project naming: `[ProjectName].Tests`
- Test class naming: `[ClassUnderTest]Tests`
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Maintain **minimum 80% code coverage**
- Include arrange-act-assert structure (comments helpful but not required)

### PowerShell Scripts
- Follow **PowerShell best practices** (approved verbs: Get-, Set-, New-, etc.)
- Include **comment-based help** for all scripts
- Use **parameter validation attributes** (`[Parameter(Mandatory)]`, `[ValidateNotNullOrEmpty()]`)
- Write **informative output** messages (`Write-Host`, `Write-Warning`, `Write-Error`)
- Handle errors gracefully with **try-catch blocks**

## GitHub Actions Workflows

### Workflow File Standards
- Use **kebab-case** naming: `pr-build.yml`, `feature-build.yml`
- Descriptive **job names**: `build-and-test`, `docker-build-and-push`
- Include **comments** explaining complex steps
- Use **job outputs** for passing data between jobs

### GitVersion Integration Requirements
- **ALWAYS** use `fetch-depth: 0` in checkout actions (GitVersion needs full history)
- Use GitVersion outputs, not hardcoded versions:
  - `semVer` - Full semantic version (e.g., `1.2.3-alpha.5`)
  - `majorMinorPatch` - Version without pre-release (e.g., `1.2.3`)
  - `assemblySemVer` - Assembly version (e.g., `1.2.3.0`)
  - `informationalVersion` - Full version with metadata

### Workflow Best Practices
- Cache NuGet packages: `actions/cache@v4`
- Use **GitHub secrets** for sensitive data (never hardcode)
- Include **version information** in Docker image tags
- Use **job concurrency** controls to prevent duplicate runs
- Add **timeout** values for long-running jobs

## File Organization

```
/
├── .github/
│   ├── workflows/              # GitHub Actions workflows
│   ├── scripts/                # Workflow support scripts (PowerShell)
│   └── copilot-instructions.md # This file
├── docs/                       # All documentation
│   ├── GITVERSION-QUICK-REFERENCE.md  # GitVersion daily reference
│   ├── GITVERSION-TEST-WALKTHROUGH.md # GitVersion testing guide
│   └── ...other guides...
├── scripts/                    # Setup and automation scripts
├── src/                        # Source code
│   └── Ormico.DbPatchManager.sln
├── GitVersion.yml              # GitVersion configuration (DO NOT MODIFY without request)
└── README.md                   # Main entry point (keep high-level)
```

## Docker Practices

### Container Image Standards
- Base images: `mcr.microsoft.com/dotnet/aspnet:8.0` for runtime
- Use **multi-stage builds** for smaller images
- Tag with **GitVersion outputs** (never hardcode versions)
- Include both **specific version** and `latest` tags
- Set **version labels** in Dockerfile

### Dockerfile Best Practices
- Minimize layers (combine RUN commands where logical)
- Use `.dockerignore` to exclude unnecessary files
- Run as **non-root user** for security
- Use specific base image versions (not `latest`)

## Common Development Patterns

### Creating New Features
1. Create feature branch: `feature/descriptive-name`
2. Develop with appropriate tests
3. Use `+semver: none` for commits that shouldn't bump version
4. Use `+semver: minor` for new feature completion
5. Create PR to main (pr-build workflow validates)
6. Merge after approval

### Fixing Bugs (All Types)
1. Create feature branch: `feature/fix-description`
2. Include regression test
3. Use `+semver: patch` for bug fixes (if not automatically detected)
4. For urgent fixes, fast-track the PR review process
5. PR to main as normal
6. Merge after approval

## Anti-Patterns to AVOID

### Version Management
- ❌ Don't manually edit or reference `version.json` (deprecated file)
- ❌ Don't use hardcoded versions anywhere
- ❌ Don't suggest creating version-related PowerShell scripts
- ❌ Don't modify `GitVersion.yml` without explicit request

### Testing
- ❌ Don't skip tests to "save time"
- ❌ Don't commit commented-out tests
- ❌ Don't use `Thread.Sleep()` for timing issues (use proper async patterns)

### Security
- ❌ Don't commit secrets, API keys, or passwords
- ❌ Don't disable security scanning
- ❌ Don't use `http://` URLs (use `https://`)
- ❌ Don't store credentials in code or config files

### Documentation
- ❌ Don't create temporary summary documents
- ❌ Don't create "completion" or "update" documents
- ❌ Don't duplicate information across multiple documents
- ❌ Don't create documents that won't have ongoing value

### Code Quality
- ❌ Don't modify main branch directly (always use PRs)
- ❌ Don't use complex or non-standard branch names
- ❌ Don't commit without meaningful commit messages
- ❌ Don't leave `TODO` or `FIXME` comments without tracking issues

## Troubleshooting Guidance

### GitVersion Issues

**Version shows 0.0.1:**
- Check that initial version tag exists: `git tag -l`
- Verify `fetch-depth: 0` in workflow checkout
- Ensure `GitVersion.yml` is in repository root

**Cannot find commit error:**
- Tag may not be pushed to remote: `git push origin --tags`
- Workflow using shallow clone (missing `fetch-depth: 0`)

**Wrong version calculated:**
- Check branch naming matches patterns (`feature/*`, not `features/*`)
- Review commit history: `git log --oneline`
- Test locally: `dotnet-gitversion /diag`

### Docker Build Failures
- Verify .NET SDK version matches project target (8.0)
- Check that all NuGet packages restore successfully
- Ensure Dockerfile `COPY` paths are correct
- Review `.dockerignore` isn't excluding needed files

### Test Failures
- Run locally first: `dotnet test`
- Check test output for specific assertion failures
- Verify test project references are correct
- Ensure test data is properly set up

## Workflow Response Guidelines

### When User Asks to Create Documentation
1. **Check existing docs first** - Can it fit in existing documentation?
2. **Ask yourself** - Will this have ongoing value, or is it temporary?
3. **If temporary** - Provide information verbally, don't create a file
4. **If permanent** - Ensure it's the right place and format

### When User Asks About Versions
1. **Always reference GitVersion** - Don't suggest manual version management
2. **Point to existing docs** - `docs/GITVERSION-QUICK-REFERENCE.md`
3. **Show commit message keywords** - How to control versions explicitly
4. **Explain branch strategy** - How branch names affect versions

### When User Asks to Update Workflows
1. **Preserve GitVersion integration** - Never remove GitVersion steps
2. **Maintain fetch-depth: 0** - Required for GitVersion
3. **Use GitVersion outputs** - Not hardcoded versions
4. **Test locally if possible** - Suggest testing with GitVersion tool

## Resources

### Primary Documentation
- **README.md** - Start here for project overview
- **docs/GITVERSION-QUICK-REFERENCE.md** - Daily developer reference
- **docs/GITVERSION-TEST-WALKTHROUGH.md** - Testing verification guide

### External Resources
- [GitVersion Documentation](https://gitversion.net/docs/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

---

## 🎯 Remember

**Quality over Quantity**: One good document > multiple mediocre documents

**Maintenance Matters**: Every file created is a file that must be maintained

**User Experience**: Clear navigation is better than comprehensive duplication

**Professional Standards**: Repository should be clean and well-organized
