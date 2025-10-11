# GitVersion CI/CD Test Walkthrough

This document provides a step-by-step walkthrough to verify that the GitVersion integration is working correctly in all workflows.

## 📋 Prerequisites

Before starting these tests, ensure:

- [ ] You're on the `feature/convert-to-gitversion` branch
- [ ] All workflow files have been updated
- [ ] You have push access to the repository
- [ ] GitHub Actions is enabled

## 🎯 Test Overview

We'll test the following scenarios:

1. ✅ Initial version tag creation
2. ✅ Feature branch versioning (alpha versions)
3. ✅ Pull Request build with GitVersion
4. ✅ Feature branch workflow with preview release
5. ✅ Release branch versioning (beta versions)
6. ✅ Hotfix branch versioning

## 📝 Test Execution

### Test 0: Verify Current State

**Purpose**: Ensure GitVersion files are in place

**Steps:**

```bash
# Check that GitVersion.yml exists
ls GitVersion.yml

# Check that workflows have been updated
git diff origin/main .GitHub/workflows/
```

**Expected Result:**

- ✅ `GitVersion.yml` file exists in repository root
- ✅ Workflow files show GitVersion actions instead of version-management.ps1

---

### Test 1: Create Initial Version Tag

**Purpose**: Establish baseline version for GitVersion

**Steps:**

```bash
# Ensure you're on the feature branch
git checkout feature/convert-to-gitversion

# Create and push the initial version tag
git tag v2.2.1
git push origin v2.2.1
```

**Expected Result:**

- ✅ Tag created successfully
- ✅ Tag visible on GitHub: `https://github.com/ormico/github-actions-ci-template/tags`

**Verification:**

```bash
# List tags
git tag -l

# Should show: v2.2.1
```

---

### Test 2: Test GitVersion Locally (Optional but Recommended)

**Purpose**: Verify GitVersion calculates versions correctly

**Prerequisites:**

```bash
# Install GitVersion tool
dotnet tool install --global GitVersion.Tool
```

**Steps:**

```bash
# Run GitVersion on current branch
dotnet-gitversion

# Check specific version output
dotnet-gitversion /showvariable SemVer
dotnet-gitversion /showvariable MajorMinorPatch
```

**Expected Result:**

- ✅ GitVersion outputs version information
- ✅ SemVer shows something like `2.2.1-alpha.X` (since you're on feature branch)
- ✅ MajorMinorPatch shows `2.2.1`

**Sample Output:**

```json
{
  "Major": 1,
  "Minor": 1,
  "Patch": 4,
  "PreReleaseTag": "alpha.5",
  "SemVer": "2.2.1-alpha.5",
  "MajorMinorPatch": "2.2.1"
}
```

---

### Test 3: Feature Branch Workflow - Manual Trigger

**Purpose**: Verify feature branch builds use GitVersion and produce alpha versions

**Steps:**

1. **Ensure you're on feature branch and push latest changes:**

   ```bash
   git checkout feature/convert-to-gitversion
   git push origin feature/convert-to-gitversion
   ```

2. **Trigger Feature Build workflow:**
   - Go to: `https://github.com/ormico/github-actions-ci-template/actions/workflows/feature-build.yml`
   - Click "Run workflow"
   - Select branch: `feature/convert-to-gitversion`
   - Check "Create preview release": `true` (optional, but recommended)
   - Click "Run workflow"

3. **Monitor the workflow execution:**
   - Click on the running workflow
   - Watch the `build-and-test` job

**Expected Results:**

✅ **In the "Determine Version" step:**

- GitVersion action runs successfully
- No errors about missing history

✅ **In the "Display GitVersion outputs" step:**

```
SemVer: 2.2.1-alpha.X
MajorMinorPatch: 2.2.1
AssemblySemVer: 2.2.1.0
InformationalVersion: 2.2.1-alpha.X+Branch.feature-convert-to-gitversion...
BranchName: feature/convert-to-gitversion
PreReleaseTag: alpha.X
```

✅ **In the "Build application" step:**

- Build succeeds with version properties set

✅ **If preview release was selected:**

- Docker image is built and pushed with alpha version tag
- Artifacts are uploaded

**Troubleshooting:**

- If version shows `0.0.1`: The initial tag (v2.2.1) may not be visible. Ensure it was pushed.
- If "fetch-depth: 0" error: Workflow needs full history, check checkout action.

---

### Test 4: Create Pull Request to Main

**Purpose**: Verify PR build workflow uses GitVersion

**Steps:**

1. **Create Pull Request:**
   - Go to GitHub repository
   - Click "Pull requests" → "New pull request"
   - Base: `main`
   - Compare: `feature/convert-to-gitversion`
   - Click "Create pull request"
   - Title: "Convert to GitVersion automated versioning"
   - Add description

2. **Monitor PR Build Workflow:**
   - PR build should automatically trigger
   - Go to "Checks" tab in the PR
   - Watch the `pr-build` workflow

3. **Check Version Calculation:**
   - Click on the running workflow
   - Look for the `version-check` job
   - Expand "Determine Version" step

**Expected Results:**

✅ **Version Check Job:**

```
SemVer: 1.1.5 (or 1.2.0 depending on commits with +semver: keywords)
MajorMinorPatch: 1.1.5
```

✅ **Build and Test Job:**

- Uses GitVersion outputs
- Build succeeds with proper version properties
- Tests run successfully

✅ **Docker Build Job:**

- Docker image builds with GitVersion tag
- No push (PR builds don't push)

✅ **PR Summary Comment:**

- Comment posted to PR with build status
- All checks should be green

**What to Verify in Logs:**

```
Run gittools/actions/gitversion/execute@v0.10.2
  with:
    useConfigFile: true
    configFilePath: GitVersion.yml
...
GitVersion executed successfully
```

**Troubleshooting:**

- If version validation fails: This is now handled by GitVersion, not manual validation
- If build fails with version error: Check that GitVersion outputs are correctly referenced

---

### Test 5: Test with Commit Message Keywords

**Purpose**: Verify `+semver:` keywords control version increments

**Steps:**

1. **Make a commit with explicit version control:**

   ```bash
   # On your feature branch
   git checkout feature/convert-to-gitversion
   
   # Make a small change
   echo "# Test" >> test.txt
   
   # Commit with minor increment keyword
   git commit -m "Add test file +semver: minor"
   
   # Push
   git push origin feature/convert-to-gitversion
   ```

2. **Check version calculation locally:**

   ```bash
   dotnet-gitversion /showvariable SemVer
   ```

3. **Verify in GitHub Actions:**
   - PR build should automatically re-run
   - Check the version in logs

**Expected Results:**

✅ **Before keyword commit:**

- Version: `2.2.1-alpha.X`

✅ **After `+semver: minor` commit:**

- Version: `1.2.0-alpha.X` (minor incremented)

✅ **With `+semver: patch`:**

- Version: `1.1.5-alpha.X` (patch incremented)

✅ **With `+semver: major`:**

- Version: `2.0.0-alpha.X` (major incremented)

---

### Test 6: Merge to Main (Final Test)

**Purpose**: Verify main branch gets stable version (no alpha tag)

**Steps:**

1. **Ensure PR checks pass:**
   - All jobs are green
   - Reviews approved (if required)

2. **Merge the PR:**
   - Click "Merge pull request"
   - Confirm merge

3. **Check main branch version:**

   ```bash
   # Switch to main
   git checkout main
   git pull origin main
   
   # Check version
   dotnet-gitversion /showvariable SemVer
   ```

**Expected Results:**

✅ **Main branch version:**

- Clean semantic version: `1.1.5` (or `1.2.0` if minor increment was used)
- **No** `-alpha` suffix
- **No** pre-release tag

✅ **Future commits on main:**

- Each commit increments patch version by default
- Use `+semver:` keywords for minor/major increments

---

### Test 7: Create Release Branch (Optional)

**Purpose**: Verify release branch gets beta versions

**Steps:**

1. **Create release branch from main:**

   ```bash
   git checkout main
   git pull origin main
   git checkout -b release/2024-10-05-01
   git push origin release/2024-10-05-01
   ```

2. **Monitor Release Workflow:**
   - Go to Actions tab
   - Watch `Release Creation` workflow
   - Click on running workflow

3. **Check version in logs:**
   - Look for "Determine Version" step in `build-release` job

**Expected Results:**

✅ **Release branch version:**

- Beta version: `1.1.5-beta.1` (or `1.2.0-beta.1`)
- Version increments with each commit on release branch

✅ **Docker images created with:**

- Version tag: `1.1.5-beta.1`
- Release tag: `v1.1.5`
- Latest tag: `latest`

✅ **GitHub Release created:**

- Release notes generated
- Artifacts attached

---

### Test 8: Create Hotfix Branch (Optional)

**Purpose**: Verify hotfix branch version handling

**Steps:**

1. **Create hotfix branch:**

   ```bash
   git checkout main
   git checkout -b hotfix/critical-fix
   
   # Make a fix
   echo "fix" > hotfix.txt
   git add hotfix.txt
   git commit -m "Fix critical issue +semver: patch"
   
   # Push
   git push origin hotfix/critical-fix
   ```

2. **Create PR to main:**
   - Create PR from hotfix branch to main
   - Monitor PR build

**Expected Results:**

✅ **Hotfix branch version:**

- Beta during development: `1.1.6-beta.1`
- Patch increment from main version

✅ **After merge to main:**

- Stable version: `1.1.6`
- No beta suffix

---

## 🎉 Success Criteria

All tests are successful if:

- [x] **Test 1**: Initial tag created (v2.2.1)
- [x] **Test 2**: GitVersion tool works locally
- [x] **Test 3**: Feature workflow produces alpha versions
- [x] **Test 4**: PR build uses GitVersion successfully
- [x] **Test 5**: Commit keywords control version increments
- [x] **Test 6**: Main branch has clean versions (no pre-release)
- [x] **Test 7**: Release branch produces beta versions
- [x] **Test 8**: Hotfix branch increments patch correctly

## 📊 Version Summary Table

After all tests, you should see these version patterns:

| Branch Type | Example Branch | Version Pattern | Example Version |
|------------|----------------|-----------------|-----------------|
| Feature | `feature/convert-to-gitversion` | `X.Y.Z-alpha.N` | `2.2.1-alpha.5` |
| Main | `main` | `X.Y.Z` | `1.1.5` |
| Release | `release/2024-10-05-01` | `X.Y.Z-beta.N` | `1.1.5-beta.1` |
| Hotfix | `hotfix/critical-fix` | `X.Y.Z-beta.N` | `1.1.6-beta.1` |

## 🔍 Common Issues and Solutions

### Issue 1: Version shows 0.0.1

**Cause**: No version tag found in repository
**Solution**:

```bash
git tag v2.2.1
git push origin v2.2.1
```

### Issue 2: GitVersion action fails with "Cannot find commit"

**Cause**: Shallow clone (not enough history)
**Solution**: Ensure workflow uses `fetch-depth: 0` in checkout action

### Issue 3: Wrong version calculated

**Cause**: Branch name doesn't match patterns in GitVersion.yml
**Solution**: Ensure branch follows pattern:

- `feature/*` for features
- `release/*` for releases
- `hotfix/*` for hotfixes

### Issue 4: Build fails with "version property not found"

**Cause**: Workflow references old version output variable
**Solution**: Ensure workflow uses GitVersion outputs:

- `steps.gitversion.outputs.semVer`
- `steps.gitversion.outputs.majorMinorPatch`

### Issue 5: No pre-release tag on feature branch

**Cause**: GitVersion configuration issue
**Solution**: Verify `GitVersion.yml` has correct branch configuration

## 📝 Test Results Log

Use this section to record your test results:

### Test 1: Initial Version Tag

- Date/Time: _______________
- Tag Created: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 2: Local GitVersion

- Date/Time: _______________
- Version Calculated: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 3: Feature Branch Workflow

- Date/Time: _______________
- Workflow Run ID: _______________
- Version Produced: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 4: PR Build

- Date/Time: _______________
- PR Number: _______________
- Version Calculated: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 5: Commit Keywords

- Date/Time: _______________
- Keyword Used: _______________
- Version Before: _______________
- Version After: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 6: Merge to Main

- Date/Time: _______________
- Final Main Version: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 7: Release Branch (Optional)

- Date/Time: _______________
- Release Version: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

### Test 8: Hotfix Branch (Optional)

- Date/Time: _______________
- Hotfix Version: _______________
- Result: ☐ Pass ☐ Fail
- Notes: _______________

## 🎓 What You Learned

After completing these tests, you've verified:

✅ GitVersion automatically calculates versions from Git history  
✅ Different branch types get appropriate version formats  
✅ Commit message keywords provide explicit version control  
✅ All workflows integrate seamlessly with GitVersion  
✅ No manual version file updates are needed  
✅ Version history is fully traceable through Git  

## 📚 Next Steps

After successful testing:

1. **Document results**: Record test outcomes above
2. **Train team**: Share GitVersion quick reference with team
3. **Update docs**: Remove references to old version.json approach
4. **Clean up**: Optionally remove version.json and old scripts
5. **Monitor**: Watch first few production releases closely

## 🆘 Getting Help

If tests fail:

1. Check GitHub Actions logs for specific errors
2. Run `dotnet-gitversion /diag` locally for diagnostics
3. Review `GitVersion.yml` configuration
4. Check [GitVersion Documentation](https://gitversion.net/docs/)
5. Review the workflow YAML files for correct GitVersion integration

---

**Last Updated**: October 5, 2025  
**Test Suite Version**: 1.0  
**GitVersion Version**: 5.x
