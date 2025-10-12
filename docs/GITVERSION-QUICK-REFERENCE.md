# GitVersion Quick Reference

## 🚀 Quick Start

### Install GitVersion Locally

```bash
dotnet tool install --global GitVersion.Tool
```

### Check Your Current Version

```bash
# In your repository
dotnet-gitversion

# Or just the semantic version
dotnet-gitversion /showvariable SemVer
```

## 📝 Commit Message Keywords

Control version increments with commit message keywords:

```bash
# Major version bump (1.2.3 → 2.0.0)
git commit -m "Breaking change: Refactor API +semver: major"
git commit -m "Complete rewrite +semver: breaking"

# Minor version bump (1.2.3 → 1.3.0)
git commit -m "Add new feature +semver: minor"
git commit -m "New dashboard component +semver: feature"

# Patch version bump (1.2.3 → 1.2.4)
git commit -m "Fix bug in calculation +semver: patch"
git commit -m "Security fix +semver: fix"

# No version bump
git commit -m "Update docs +semver: none"
git commit -m "Refactor tests +semver: skip"
```

## 🌿 Branch Versioning

| Branch Type | Version Format | Example | When to Use |
|------------|----------------|---------|-------------|
| `main` | `X.Y.Z` | `1.2.3` | Production releases |
| `feature/*` | `X.Y.Z-alpha.N` | `1.2.3-alpha.5` | New features in development |
| `release/*` | `X.Y.Z-beta.N` | `1.2.3-beta.2` | Release candidates |
| `hotfix/*` | `X.Y.Z-beta.N` | `1.2.4-beta.1` | Critical fixes |

## 🔢 Version Outputs

GitVersion provides these version formats:

```yaml
SemVer:                    1.2.3-alpha.4        # Use for: Docker tags, NuGet
MajorMinorPatch:           1.2.3                # Use for: Display, Git tags
AssemblySemVer:            1.2.3.0              # Use for: .NET AssemblyVersion
AssemblySemFileVer:        1.2.3.4              # Use for: .NET FileVersion
InformationalVersion:      1.2.3-alpha.4+...    # Use for: Detailed build info
```

## 🎯 Common Workflows

### Creating a Feature

```bash
# Create feature branch
git checkout -b feature/my-feature

# Work on your feature
git commit -m "Add initial implementation"
git commit -m "Add tests"

# Explicitly increment minor version
git commit -m "Complete feature implementation +semver: minor"

# Push and create PR
git push origin feature/my-feature
```

### Creating a Hotfix

```bash
# Create hotfix branch from main
git checkout main
git checkout -b hotfix/critical-bug

# Fix the issue
git commit -m "Fix critical security issue +semver: patch"

# Push and create PR to main
git push origin hotfix/critical-bug
```

### Creating a Release

```bash
# Create release branch
git checkout -b release/2024-10-05-01

# Make release-specific changes
git commit -m "Update version info +semver: none"

# Push to trigger release workflow
git push origin release/2024-10-05-01
```

## 🏷️ Working with Tags

### View Current Tags

```bash
git tag -l
```

### Create Initial Version Tag

```bash
# Set the baseline version
git tag v1.1.4
git push origin v1.1.4
```

### Create Release Tag

```bash
# After merging to main
git tag v1.2.0
git push origin v1.2.0
```

## 🔍 Troubleshooting

### Check What Version Will Be Generated

```bash
# Show all version variables
dotnet-gitversion

# Show specific variable
dotnet-gitversion /showvariable SemVer
dotnet-gitversion /showvariable MajorMinorPatch
```

### Debug Version Calculation

```bash
# Show detailed diagnostics
dotnet-gitversion /diag

# Show effective configuration
dotnet-gitversion /showconfig
```

### Common Issues

**"Cannot find the commit"**

```bash
# Ensure you have full history
git fetch --unshallow
```

**"No version found"**

```bash
# Create an initial tag
git tag v1.0.0
git push origin v1.0.0
```

**"Wrong version calculated"**

```bash
# Check your branch name matches patterns
# feature/*, release/*, hotfix/*
```

## 📖 Examples

### Example 1: Feature Development

```bash
# Current main version: 1.2.3
git checkout -b feature/user-auth
# Version: 1.2.3-alpha.1

git commit -m "Add login form"
# Version: 1.2.3-alpha.2

git commit -m "Add authentication logic +semver: minor"
# After merge to main: 1.3.0
```

### Example 2: Hotfix

```bash
# Current main version: 1.2.3
git checkout -b hotfix/security-fix
# Version: 1.2.4-beta.1

git commit -m "Fix XSS vulnerability +semver: patch"
# After merge to main: 1.2.4
```

### Example 3: Multiple Features

```bash
# Feature A (adds minor feature)
git commit -m "Feature A +semver: minor"  # 1.3.0

# Feature B (adds minor feature)
git commit -m "Feature B +semver: minor"  # 1.4.0

# Feature C (just patches)
git commit -m "Feature C +semver: patch"  # 1.4.1
```

## 🛠️ Local Development

### Test Version Before Pushing

```bash
# Make your commits
git commit -m "My changes +semver: minor"

# Check what version would be generated
dotnet-gitversion /showvariable SemVer

# If incorrect, amend your commit message
git commit --amend -m "My changes +semver: patch"
```

### Preview in CI/CD

All GitHub Actions workflows now show version in logs:

1. Push your branch
2. Check Actions tab
3. Look for "Display GitVersion outputs" step

## 📚 Learn More

- **GitVersion Docs**: <https://gitversion.net/docs/>
- **Configuration**: See `GitVersion.yml` in repo root
- **Migration Guide**: See `GITVERSION-MIGRATION.md`
- **Full Summary**: See `GITVERSION-CONVERSION-SUMMARY.md`

## 💡 Pro Tips

1. **Always use `+semver:`** keywords for clarity
2. **Test locally** before pushing: `dotnet-gitversion`
3. **Tag releases** on main for clean version history
4. **Branch naming** matters: follow patterns exactly
5. **Full history** required: workflows use `fetch-depth: 0`

## ❓ Quick FAQ

**Q: Do I need to update version.json?**  
A: No! GitVersion handles all versioning automatically.

**Q: What if I forget `+semver:`?**  
A: GitVersion will use default increment (usually patch for main branch).

**Q: Can I override the version?**  
A: Yes, create a Git tag with desired version: `git tag v2.0.0`

**Q: How do I see the version in build logs?**  
A: Check the "Display GitVersion outputs" step in GitHub Actions.

**Q: What happens on main branch without keywords?**  
A: Automatic patch increment (1.2.3 → 1.2.4).

---

**Last Updated**: October 5, 2025  
**GitVersion Configuration**: See `GitVersion.yml`  
**Support**: Check migration docs or GitVersion documentation
