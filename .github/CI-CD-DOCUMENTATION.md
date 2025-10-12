# GitHub Actions CI/CD Pipeline Documentation

This repository contains a comprehensive Build, Release, and Deployment framework implemented using GitHub Actions for .NET applications.

## 🏗️ Architecture Overview

The CI/CD pipeline implements a modern DevOps workflow with the following components:

- **Pull Request Builds**: Automated validation of code changes
- **Feature Branch Builds**: Manual builds with optional preview releases
- **Release Management**: Automated release creation and artifact management  
- **Environment Deployment**: Multi-stage deployment pipeline
- **Version Management**: Semantic versioning with automated validation
- **Cleanup Automation**: Automated cleanup of old artifacts and resources

## 📋 Workflows

### Core Build Workflows

#### 1. Pull Request Build (`pr-build.yml`)
**Trigger**: PR creation/updates to `main` or `release/*` branches

**Features**:
- ✅ Version increment validation (for PRs to main)
- ✅ Build and unit test execution
- ✅ Security scanning and dependency checks
- ✅ Code quality validation
- ✅ Docker build testing
- ✅ Comprehensive PR status comments

#### 2. Feature Branch Build (`feature-build.yml`)
**Trigger**: Manual (`workflow_dispatch`)

**Features**:
- ✅ Build verification same as PR builds
- ✅ Optional preview release creation
- ✅ Docker image building and pushing
- ✅ Security scanning for previews
- ✅ Branch-specific version suffixes

📖 **Documentation**: 
- **[User Guide](../docs/FEATURE-BUILD-USER-GUIDE.md)** - How to use feature builds
- **[Developer Guide](../docs/FEATURE-BUILD-DEVELOPER.md)** - Technical implementation details

#### 3. Release Creation (`release-create.yml`)
**Trigger**: Push to `release/*` branches

**Features**:
- ✅ Release branch validation
- ✅ Build and test execution
- ✅ Release artifact creation
- ✅ Docker image building with multiple tags
- ✅ GitHub release creation
- ✅ Optional development environment deployment

### Supporting Workflows

#### 4. Test Runner (`test-runner.yml`)
**Trigger**: Reusable workflow

**Features**:
- ✅ Support for unit, integration, E2E, and smoke tests
- ✅ Environment-specific test execution
- ✅ Test result publishing and reporting
- ✅ Code coverage collection

#### 5. Cleanup Automation (`cleanup.yml`)
**Trigger**: Scheduled (weekly) or manual

**Features**:
- ✅ Preview release cleanup
- ✅ Workflow artifact management
- ✅ Container image cleanup
- ✅ Merged branch removal

## 🔧 Setup Instructions

### Prerequisites

1. **GitHub Repository** with Actions enabled
2. **GitHub Container Registry** access
3. **.NET 8 SDK** (automatically configured in workflows)
4. **PowerShell** (for local script execution)
5. **GitHub CLI** (for repository setup)

### Initial Setup

1. **Clone this repository** or copy the `.github` folder to your project

2. **Configure repository settings** using the provided script:
   ```powershell
   # Run from repository root
   .\scripts\setup-repository.ps1
   
   # For dry run to see what would be configured:
   .\scripts\setup-repository.ps1 -DryRun
   ```

3. **Initialize version management**:
   - The `version.json` file is already created with initial version `1.0.0`
   - Update the version as needed for your project

4. **Configure GitHub Environments** (if not using automated setup):
   - Go to repository Settings → Environments
   - Create environments: `development`, `testing`, `staging`, `production`
   - Configure protection rules and reviewers as needed

### Branch Strategy

#### Branch Naming Conventions
- **Feature branches**: `feature/{feature-name}` or `feature/{issue-number}-{feature-name}`
- **Bug branches**: `bug/{bug-name}` or `bug/{issue-number}-{bug-name}`
- **Release branches**: `release/{YYYY-MM-DD}-{increment}` (e.g., `release/2024-03-15-01`)
- **Hotfix branches**: `hotfix/{release-id}-{hotfix-name}`

#### Branch Protection
The setup script automatically configures:
- **Main branch**: Requires PR, status checks, up-to-date branches
- **Release branches**: Requires PR for hotfixes, status checks

## 📦 Version Management

### Semantic Versioning
Uses 4-segment versioning: `Major.Minor.Patch.Build`

- **Major**: Breaking changes (manual)
- **Minor**: New features (manual)
- **Patch**: Bug fixes (manual)
- **Build**: Auto-incremented per build

### Version Operations

```powershell
# Get current version
.\.github\scripts\version-management.ps1 -Action get

# Increment version
.\.github\scripts\version-management.ps1 -Action increment -Component patch

# Create preview version
.\.github\scripts\version-management.ps1 -Action set-preview -PreviewSuffix "preview-abc123-01"

# Validate version (used in PR builds)
.\.github\scripts\version-management.ps1 -Action validate
```

### Preview Versioning
Preview releases use the format: `{version}-preview-{branch-hash}-{increment}`

Example: `1.2.3-preview-a1b2c3d4-01`

## 🚀 Usage

### Creating a Pull Request
1. Create feature branch: `git checkout -b feature/my-new-feature`
2. Make changes and update version in `version.json` (increment patch/minor/major)
3. Commit and push changes
4. Create PR to `main` branch
5. PR build will automatically validate version increment and run tests

### Creating a Preview Release
1. Push feature branch to repository
2. Go to Actions → Feature Branch Build
3. Click "Run workflow"
4. Select your branch and check "Create preview release"
5. Preview release will be created with Docker image

### Creating a Release
1. Create release branch: `git checkout -b release/2024-03-15-01`
2. Push branch: `git push origin release/2024-03-15-01`
3. Release workflow automatically triggers
4. Creates GitHub release, Docker images, and optional dev deployment

### Running Tests
Tests can be run manually using the test runner workflow or automatically as part of other workflows.

## 🐳 Docker Images

### Image Tagging Strategy
- **Preview releases**: `{registry}/{owner}/simpleweatherlist.web:{preview-version}`
- **Release versions**: `{registry}/{owner}/simpleweatherlist.web:{version}`
- **Release tags**: `{registry}/{owner}/simpleweatherlist.web:v{version}`
- **Latest**: `{registry}/{owner}/simpleweatherlist.web:latest`

### Registry
Images are pushed to GitHub Container Registry: `ghcr.io`

## 🧹 Cleanup and Maintenance

### Automated Cleanup
The cleanup workflow runs weekly and removes:
- Preview releases older than 7 days
- Workflow artifacts older than 30 days  
- Container images older than 90 days
- Merged feature branches older than 30 days

### Manual Cleanup
```bash
# Run cleanup workflow manually
gh workflow run cleanup.yml

# Run with custom retention periods
gh workflow run cleanup.yml \
  -f cleanup_previews=3 \
  -f cleanup_artifacts=15 \
  -f cleanup_images=60 \
  -f dry_run=true
```

## 🔒 Security

### Security Scanning
- Dependency vulnerability checking
- Container security scanning
- Code quality analysis
- Security scan results in PR status checks

### Secrets Management
Required secrets for full functionality:
- `GITHUB_TOKEN` (automatically provided)
- Additional deployment secrets as needed per environment

## 📊 Monitoring and Reporting

### Test Reporting
- Unit test results published to PR and Actions summary
- Code coverage tracking with Codecov integration
- Test history and trends

### Build Metrics
- Build success/failure rates
- Build duration tracking
- Deployment success metrics

## 🛠️ Customization

### Adapting for Your Project

1. **Update project paths** in workflows if your project structure differs
2. **Modify test commands** in workflows for your testing framework
3. **Customize deployment logic** for your target environments
4. **Adjust retention periods** in cleanup workflow
5. **Configure notification preferences** for build results

### Environment Configuration

Each environment can be configured with:
- Deployment approval requirements
- Environment-specific secrets
- Custom deployment scripts
- Environment protection rules

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/)
- [.NET Container Publishing](https://docs.microsoft.com/en-us/dotnet/core/docker/publish-as-container)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)

## 🤝 Contributing

When contributing to this CI/CD framework:

1. Test changes in a fork first
2. Update documentation for any workflow changes
3. Follow the established branching strategy
4. Ensure version increments are appropriate
5. Update this README for any new features

## 📄 License

This CI/CD framework is provided as-is for educational and production use. Adapt as needed for your specific requirements.
