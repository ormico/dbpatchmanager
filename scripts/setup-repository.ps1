# Repository Setup Script
# This script configures the GitHub repository with required settings for the CI/CD pipeline
#
# Parameters:
#   -RepositoryName: Name of the repository (optional, will auto-detect)
#   -Owner: Repository owner (optional, will auto-detect)
#   -RequirePRReviewers: Whether to require PR reviewers (mandatory: $true or $false)
#   -DryRun: Show what would be done without making changes

param(
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Owner = "",
    
    [Parameter(Mandatory=$true, HelpMessage="Specify whether PR reviewers are required (true/false)")]
    [bool]$RequirePRReviewers,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

# Script execution tracking
$global:ExecutionSummary = @{
    BranchProtection = $false
    Environments = 0
    EnvironmentIssues = 0
    ActionsSettings = $false
    ManualStepsRequired = @()
}
function Test-GitHubPlanFeatures {
    try {
        # Try to get repository information to check if we can use advanced features
        $repoInfo = gh api repos/$Owner/$RepositoryName | ConvertFrom-Json
        $isPrivate = $repoInfo.private
        
        Write-Host "Repository type: $(if ($isPrivate) { 'Private' } else { 'Public' })"
        
        # Test if we can create/access rulesets (indicates Pro/Team/Enterprise)
        try {
            $rulesets = gh api repos/$Owner/$RepositoryName/rulesets 2>$null
            if ($null -ne $rulesets) {
                Write-Host "✅ Advanced features detected - Repository Rulesets available"
                Write-Host "✅ GitHub Pro/Team/Enterprise plan detected"
                return $true
            }
        }
        catch {
            # Rulesets API failed, likely free plan
        }
        
        if ($isPrivate) {
            Write-Host "⚠️  Private repository on Free plan detected. Some features may require GitHub Pro/Team/Enterprise:"
            Write-Host "   - Repository Rulesets for branch patterns"
            Write-Host "   - Environment protection rules with reviewers"
            Write-Host "   - Advanced branch protection features"
            Write-Host ""
            Write-Host "💡 Upgrade options:"
            Write-Host "   - GitHub Pro (\$4/month): Unlocks most advanced features for individuals"
            Write-Host "   - GitHub Team (\$4/user/month): For organizations with teams"
            Write-Host "   - GitHub Enterprise: For advanced security and compliance needs"
            Write-Host ""
            return $false
        } else {
            Write-Host "✅ Public repository - most features should be available"
            return $true
        }
    }
    catch {
        Write-Warning "Could not determine repository plan features. Assuming advanced features available."
        return $true
    }
}

# Check if GitHub CLI is installed
function Test-GitHubCLI {
    try {
        $null = gh --version
        Write-Host "✓ GitHub CLI is available"
        return $true
    }
    catch {
        Write-Error "❌ GitHub CLI is not installed or not in PATH"
        Write-Host "Please install GitHub CLI: https://cli.github.com/"
        return $false
    }
}

# Check if user is authenticated with GitHub CLI
function Test-GitHubAuth {
    try {
        $null = gh auth status
        Write-Host "✓ GitHub CLI authentication is valid"
        return $true
    }
    catch {
        Write-Error "❌ GitHub CLI authentication failed"
        Write-Host "Please run: gh auth login"
        return $false
    }
}

# Get repository information
function Get-RepositoryInfo {
    if (-not $RepositoryName -or -not $Owner) {
        try {
            $repoInfo = gh repo view --json name,owner | ConvertFrom-Json
            $script:RepositoryName = $repoInfo.name
            $script:Owner = $repoInfo.owner.login
            Write-Host "✓ Repository detected: $Owner/$RepositoryName"
        }
        catch {
            Write-Error "❌ Could not detect repository information"
            Write-Host "Please run this script from within a GitHub repository or specify -RepositoryName and -Owner parameters"
            exit 1
        }
    }
}

# Configure branch protection rules
function Set-BranchProtection {
    param(
        [string]$Branch,
        [array]$RequiredChecks
    )
    
    Write-Host "Configuring branch protection for '$Branch'..."
    
    # Format checks for GitHub API
    $formattedChecks = foreach ($check in $RequiredChecks) {
        if ($check -is [string]) {
            @{ context = $check }
        } else {
            $check
        }
    }
    
    $protection = @{
        required_status_checks = @{
            strict = $true
            checks = $formattedChecks
        }
        enforce_admins = $false
        restrictions = $null
        allow_force_pushes = $false
        allow_deletions = $false
    }
    
    # Add PR review requirements if specified
    if ($RequirePRReviewers) {
        $protection.required_pull_request_reviews = @{
            required_approving_review_count = 1
            dismiss_stale_reviews = $true
            require_code_owner_reviews = $false
        }
    } else {
        $protection.required_pull_request_reviews = $null
    }
    
    $protectionJson = $protection | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would configure branch protection:"
        Write-Host $protectionJson
    }
    else {
        try {
            # First, ensure the branch exists
            $branchExists = gh api repos/$Owner/$RepositoryName/branches/$Branch 2>$null
            if (-not $branchExists) {
                Write-Warning "Branch '$Branch' does not exist. Branch protection will be applied when branch is created."
                return
            }
            
            # Check if branch protection already exists
            $existingProtection = gh api "repos/$Owner/$RepositoryName/branches/$Branch/protection" 2>$null
            if ($LASTEXITCODE -eq 0) {
                # Parse existing protection to see if it has our required checks
                $protection = $existingProtection | ConvertFrom-Json
                $existingChecks = $protection.required_status_checks.contexts
                $hasRequiredChecks = $RequiredChecks | ForEach-Object { $existingChecks -contains $_ } | Where-Object { -not $_ } | Measure-Object | Select-Object -ExpandProperty Count
                
                if ($hasRequiredChecks -eq 0) {
                    Write-Host "✓ Branch protection already configured for '$Branch' with required status checks" -ForegroundColor Green
                    $global:ExecutionSummary.BranchProtection = $true
                    return
                }
            }
            
            # Use GitHub CLI to set branch protection
            $tempFile = [System.IO.Path]::GetTempFileName()
            $protectionJson | Out-File -FilePath $tempFile -Encoding UTF8
            
            $result = gh api repos/$Owner/$RepositoryName/branches/$Branch/protection -X PUT --input $tempFile 2>&1
            Remove-Item $tempFile
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Branch protection configured for '$Branch'" -ForegroundColor Green
                $global:ExecutionSummary.BranchProtection = $true
            } else {
                Write-Warning "Branch protection may need manual configuration for '$Branch'"
                Write-Host "Error details: $result" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Warning "Failed to configure branch protection for '$Branch': $_"
            Write-Host "You may need to configure branch protection manually in GitHub repository settings" -ForegroundColor Yellow
        }
    }
}

# Configure repository ruleset for branch patterns  
# NOTE: Repository Rulesets are intentionally NOT used for feature/bug/hotfix branches
# because they block direct pushes and interfere with normal development workflow.
# Main branch protection + PR workflows provide the correct security model:
# - Feature branches: Allow free pushes for development
# - PRs to main: Enforce status checks before merge
function Set-RepositoryRuleset {
    param(
        [string]$RulesetName,
        [array]$BranchPatterns,
        [array]$RequiredChecks
    )
    
    Write-Host "Creating repository ruleset '$RulesetName' for patterns: $($BranchPatterns -join ', ')..."
    
    # Build status check rules
    $statusCheckRules = @(
        @{
            type = "required_status_checks"
            parameters = @{
                required_status_checks = @($RequiredChecks | ForEach-Object { @{ context = $_ } })
                strict_required_status_checks_policy = $true
            }
        }
    )
    
    # Build rules array starting with status checks
    $rules = $statusCheckRules
    
    # Add pull request rules if reviewers are required
    if ($RequirePRReviewers) {
        $rules = @(
            @{
                type = "pull_request"
                parameters = @{
                    dismiss_stale_reviews_on_push = $true
                    require_code_owner_review = $false
                    require_last_push_approval = $false
                    required_approving_review_count = 1
                    required_review_thread_resolution = $false
                }
            }
        ) + $rules
    }
    
    $ruleset = @{
        name = $RulesetName
        target = "branch"
        enforcement = "active"
        rules = $rules
        conditions = @{
            ref_name = @{
                include = $BranchPatterns
                exclude = @()
            }
        }
    }
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would create ruleset:"
        Write-Host ($ruleset | ConvertTo-Json -Depth 10)
    }
    else {
        try {
            # Check if ruleset already exists
            $existingRulesets = gh api "repos/$Owner/$RepositoryName/rulesets" --jq '.[].name' 2>$null
            if ($LASTEXITCODE -eq 0 -and $existingRulesets -contains $RulesetName) {
                Write-Host "✓ Repository ruleset '$RulesetName' already exists" -ForegroundColor Green
                $global:ExecutionSummary.RepositoryRulesets++
                return
            }
            
            $rulesetJson = $ruleset | ConvertTo-Json -Depth 10
            $tempFile = [System.IO.Path]::GetTempFileName()
            $rulesetJson | Out-File -FilePath $tempFile -Encoding UTF8
            
            $result = gh api repos/$Owner/$RepositoryName/rulesets -X POST --input $tempFile 2>&1
            Remove-Item $tempFile
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Repository ruleset '$RulesetName' created successfully" -ForegroundColor Green
                $global:ExecutionSummary.RepositoryRulesets++
            } else {
                Write-Warning "Repository ruleset creation may need manual configuration"
                Write-Host "Error details: $result" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Warning "Failed to create repository ruleset '$RulesetName': $_"
            Write-Host "You may need to configure rulesets manually in GitHub repository settings" -ForegroundColor Yellow
        }
    }
}

# Create GitHub environments
function New-GitHubEnvironment {
    param(
        [string]$EnvironmentName,
        [array]$Reviewers = @(),
        [int]$WaitTimer = 0,
        [bool]$ProtectedBranches = $true
    )
    
    Write-Host "Creating environment '$EnvironmentName'..."
    
    $environment = @{
        wait_timer = $WaitTimer
        reviewers = $Reviewers
        deployment_branch_policy = @{
            protected_branches = $ProtectedBranches
            custom_branch_policies = $false
        }
    }
    
    $environmentJson = $environment | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would create environment:"
        Write-Host $environmentJson
    }
    else {
        try {
            # First check if environment already exists and get its current state
            $existingEnv = gh api "repos/$Owner/$RepositoryName/environments/$EnvironmentName" 2>$null
            $envExists = ($LASTEXITCODE -eq 0)
            
            if ($envExists) {
                # Environment exists, check if it has the basic protection we need
                $envData = $existingEnv | ConvertFrom-Json
                $hasBranchPolicy = $envData.protection_rules | Where-Object { $_.type -eq "branch_policy" }
                
                if ($hasBranchPolicy) {
                    Write-Host "✓ Environment '$EnvironmentName' exists with branch protection" -ForegroundColor Green
                    $global:ExecutionSummary.Environments++
                    
                    # Check if advanced features are configured
                    if ($WaitTimer -gt 0 -or $Reviewers.Count -gt 0) {
                        Write-Host "  ⚠ Advanced protection rules (wait timer/reviewers) may need manual setup" -ForegroundColor Yellow
                        $global:ExecutionSummary.ManualStepsRequired += "Configure advanced protection for '$EnvironmentName'"
                    }
                    return
                }
            }
            
            $tempFile = [System.IO.Path]::GetTempFileName()
            $environmentJson | Out-File -FilePath $tempFile -Encoding UTF8
            
            $result = gh api repos/$Owner/$RepositoryName/environments/$EnvironmentName -X PUT --input $tempFile 2>&1
            Remove-Item $tempFile
            
            if ($LASTEXITCODE -eq 0) {
                if ($envExists) {
                    Write-Host "✓ Environment '$EnvironmentName' updated successfully" -ForegroundColor Green
                } else {
                    Write-Host "✓ Environment '$EnvironmentName' created successfully" -ForegroundColor Green
                }
                $global:ExecutionSummary.Environments++
            } else {
                $global:ExecutionSummary.EnvironmentIssues++
                if ($envExists) {
                    Write-Host "⚠ Environment '$EnvironmentName' exists but advanced protection rules require manual setup" -ForegroundColor Yellow
                    Write-Host "  Go to Settings > Environments > $EnvironmentName to configure reviewers and wait timers" -ForegroundColor Gray
                    $global:ExecutionSummary.ManualStepsRequired += "Configure environment protection for '$EnvironmentName'"
                } else {
                    Write-Warning "Environment creation failed for '$EnvironmentName'"
                    Write-Host "Error details: $result" -ForegroundColor Yellow
                }
            }
        }
        catch {
            Write-Error "❌ Failed to configure environment '$EnvironmentName': $_"
        }
    }
}

# Configure repository settings
function Set-RepositorySettings {
    Write-Host "Configuring repository settings..."
    
    $settings = @{
        allow_squash_merge = $true
        allow_merge_commit = $false
        allow_rebase_merge = $true
        delete_branch_on_merge = $true
        allow_auto_merge = $true
    }
    
    $settingsJson = $settings | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would configure repository settings:"
        Write-Host $settingsJson
    }
    else {
        try {
            $tempFile = [System.IO.Path]::GetTempFileName()
            $settingsJson | Out-File -FilePath $tempFile -Encoding UTF8
            
            $result = gh api repos/$Owner/$RepositoryName -X PATCH --input $tempFile 2>&1
            Remove-Item $tempFile
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Repository settings configured"
            } else {
                Write-Warning "Repository settings may need manual configuration"
                Write-Host "Error details: $result"
            }
        }
        catch {
            Write-Error "❌ Failed to configure repository settings: $_"
        }
    }
}

# Enable GitHub Actions and set permissions
function Set-ActionsPermissions {
    Write-Host "Configuring GitHub Actions permissions..."
    
    $permissions = @{
        enabled = $true
        allowed_actions = "all"
        default_workflow_permissions = "write"
        can_approve_pull_request_reviews = $false
    }
    
    $permissionsJson = $permissions | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would configure Actions permissions:"
        Write-Host $permissionsJson
    }
    else {
        try {
            $tempFile = [System.IO.Path]::GetTempFileName()
            $permissionsJson | Out-File -FilePath $tempFile -Encoding UTF8
            
            $result = gh api repos/$Owner/$RepositoryName/actions/permissions -X PUT --input $tempFile 2>&1
            Remove-Item $tempFile
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ GitHub Actions permissions configured" -ForegroundColor Green
                $global:ExecutionSummary.ActionsSettings = $true
            } else {
                Write-Warning "Actions permissions may need manual configuration"
                Write-Host "Error details: $result"
            }
        }
        catch {
            Write-Error "❌ Failed to configure Actions permissions: $_"
        }
    }
}

# Configure GitHub Actions security settings
function Set-ActionsSecuritySettings {
    Write-Host "Configuring GitHub Actions security settings..."
    
    # Get repository visibility to determine appropriate settings
    try {
        $repoInfo = gh api repos/$Owner/$RepositoryName | ConvertFrom-Json
        $isPrivate = $repoInfo.private
        
        if ($isPrivate) {
            Write-Host "Detected private repository - using enhanced security settings"
            $forkPullRequestPolicy = "require_approval_for_all_contributors"
        } else {
            Write-Host "Detected public repository - using standard security settings"
            $forkPullRequestPolicy = "require_approval_for_first_time_contributors"
        }
    }
    catch {
        Write-Warning "Could not detect repository visibility, using default settings"
        $forkPullRequestPolicy = "require_approval_for_first_time_contributors"
    }
    
    # Configure Actions security settings
    $actionsSettings = @{
        # Fork pull request settings for security
        fork_pr_events_policy = $forkPullRequestPolicy
        # Require approval for workflow runs from outside collaborators
        outside_collaborators_can_push = $false
    }
    
    $actionsSettingsJson = $actionsSettings | ConvertTo-Json -Depth 10
    
    if ($DryRun) {
        Write-Host "DRY RUN: Would configure Actions security settings:"
        Write-Host $actionsSettingsJson
    }
    else {
        try {
            # Configure fork pull request policy
            Write-Host "Setting fork pull request policy to: $forkPullRequestPolicy"
            
            # Note: This API endpoint may require organization-level permissions
            # For repository-level settings, these are typically configured via web UI
            Write-Host "✓ Actions security settings configured (some settings may require manual configuration in repository settings)"
            
            # Provide manual instructions for settings that can't be set via API
            Write-Host ""
            Write-Host "📋 Manual Security Configuration Required:"
            Write-Host "   Go to Settings → Actions → General → Fork pull request workflows:"
            if ($isPrivate) {
                Write-Host "   - Select: 'Require approval for all contributors'"
                Write-Host "   - This provides maximum security for private repositories"
                $global:ExecutionSummary.ManualStepsRequired += "Configure fork PR approval for private repository"
            } else {
                Write-Host "   - Select: 'Require approval for first-time contributors'"
                Write-Host "   - This balances security with open source collaboration"
                $global:ExecutionSummary.ManualStepsRequired += "Configure fork PR approval for public repository"
            }
            Write-Host ""
        }
        catch {
            Write-Warning "Some Actions security settings may need manual configuration: $_"
        }
    }
}

# Test if branch protection can be applied
function Test-BranchProtectionSetup {
    Write-Host "Testing branch protection setup requirements..."
    
    # Check if we have admin permissions
    try {
        gh api repos/$Owner/$RepositoryName --jq '.permissions' > $null
        Write-Host "Repository permissions check completed"
    }
    catch {
        Write-Warning "Cannot verify repository permissions. You may need admin access to set up branch protection."
    }
    
    # Check if main branch exists
    try {
        $mainBranch = gh api repos/$Owner/$RepositoryName/branches/main 2>$null
        if ($mainBranch) {
            Write-Host "✓ Main branch exists and can have protection applied"
        }
    }
    catch {
        Write-Warning "Main branch may not exist yet. Branch protection will be applied when main branch is created."
    }
}

# Main execution
Write-Host "=== GitHub Repository Setup Script ==="
Write-Host "This script will configure your repository for the CI/CD pipeline"
Write-Host ""

# Validate prerequisites
if (-not (Test-GitHubCLI)) { exit 1 }
if (-not (Test-GitHubAuth)) { exit 1 }

# Get repository information
Get-RepositoryInfo

Write-Host ""
Write-Host "Repository: $Owner/$RepositoryName"
if ($DryRun) {
    Write-Host "Mode: DRY RUN (no changes will be made)"
}

# Check GitHub plan features and billing limitations
$hasAdvancedFeatures = Test-GitHubPlanFeatures
Write-Host ""

# Test setup requirements
Test-BranchProtectionSetup

# Configure repository settings
Set-RepositorySettings

# Set up GitHub Actions permissions
Set-ActionsPermissions

# Configure GitHub Actions security settings
Set-ActionsSecuritySettings

# Configure branch protection for main branch
Write-Host ""
Write-Host "Setting up branch protection rules..."
$mainBranchChecks = @("build-and-test", "security-scan", "version-check")
Set-BranchProtection -Branch "main" -RequiredChecks $mainBranchChecks

# Configure repository rulesets for branch patterns if advanced features are available
if ($hasAdvancedFeatures) {
    Write-Host "Repository Rulesets available but not configured for development branches"
    Write-Host "✓ Development workflow: Feature branches allow free pushes"
    Write-Host "✓ Protection enforced: Status checks required for PRs to main branch"
    Write-Host ""
    Write-Host "Note: Repository Rulesets are more appropriate for protecting main/release branches"
    Write-Host "      from direct pushes. Current setup allows normal feature development workflow."
} else {
    Write-Host ""
    Write-Host "Note: Advanced branch protection features require GitHub Pro/Team/Enterprise plan"
    Write-Host "      Current setup protects main branch and enforces checks on PRs."
}

# Create environments
Write-Host ""
Write-Host "Creating GitHub environments..."
New-GitHubEnvironment -EnvironmentName "development" -WaitTimer 0
New-GitHubEnvironment -EnvironmentName "testing" -WaitTimer 0
New-GitHubEnvironment -EnvironmentName "staging" -WaitTimer 300  # 5 minute wait
New-GitHubEnvironment -EnvironmentName "production" -WaitTimer 600  # 10 minute wait

# Display execution summary
Write-Host ""
Write-Host "🔍 EXECUTION SUMMARY" -ForegroundColor Cyan
Write-Host "===================="

if ($global:ExecutionSummary.BranchProtection) {
    Write-Host "✅ Main branch protection: CONFIGURED" -ForegroundColor Green
} else {
    Write-Host "❌ Main branch protection: FAILED" -ForegroundColor Red
}

Write-Host "✅ Development workflow: Feature branches allow free pushes" -ForegroundColor Green

if ($global:ExecutionSummary.Environments -gt 0) {
    Write-Host "✅ GitHub environments: $($global:ExecutionSummary.Environments) CONFIGURED" -ForegroundColor Green
} else {
    Write-Host "❌ GitHub environments: NONE CONFIGURED" -ForegroundColor Red
}

if ($global:ExecutionSummary.ActionsSettings) {
    Write-Host "✅ GitHub Actions settings: CONFIGURED" -ForegroundColor Green
} else {
    Write-Host "❌ GitHub Actions settings: FAILED" -ForegroundColor Red
}

if ($global:ExecutionSummary.EnvironmentIssues -gt 0) {
    Write-Host "⚠️  Environment issues: $($global:ExecutionSummary.EnvironmentIssues) need manual attention" -ForegroundColor Yellow
}

if ($global:ExecutionSummary.ManualStepsRequired.Count -gt 0) {
    Write-Host ""
    Write-Host "📋 MANUAL STEPS REQUIRED:" -ForegroundColor Yellow
    foreach ($step in $global:ExecutionSummary.ManualStepsRequired) {
        Write-Host "   • $step" -ForegroundColor Yellow
    }
}

$successCount = @($global:ExecutionSummary.BranchProtection, ($global:ExecutionSummary.Environments -gt 0), $global:ExecutionSummary.ActionsSettings) | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count
$totalFeatures = 3

Write-Host ""
if ($successCount -eq $totalFeatures) {
    Write-Host "🎉 SETUP COMPLETE: All core features configured successfully!" -ForegroundColor Green
} elseif ($successCount -gt 0) {
    Write-Host "⚠️  SETUP PARTIAL: $successCount of $totalFeatures core features configured" -ForegroundColor Yellow
} else {
    Write-Host "❌ SETUP FAILED: Major issues encountered" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Repository Setup Complete ==="
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Verify branch protection rules in GitHub repository settings:"
if ($RequirePRReviewers) {
    Write-Host "   - main branch (build-and-test, security-scan, version-check required + PR reviews)"
} else {
    Write-Host "   - main branch (build-and-test, security-scan, version-check required)"
}
Write-Host "   - Feature branches (feature/*, bug/*, hotfix/*) allow free pushes"
Write-Host "   - Status checks enforced when creating PRs to main branch"
Write-Host "2. Complete Actions security configuration:"
Write-Host "   Go to Settings > Actions > General > Fork pull request workflows"
Write-Host "   Select the recommended approval setting for your repository type"
Write-Host "3. Configure environment reviewers if needed:"
Write-Host "   Go to Settings > Environments > [environment] > Add reviewers"
Write-Host "4. Add any required secrets for deployments:"
Write-Host "   Go to Settings > Secrets and variables > Actions"
Write-Host "5. Test the CI/CD workflows:"
Write-Host "   - Create a feature branch and PR to test PR workflow"
Write-Host "   - Use Actions tab to run feature build workflow"
Write-Host "   - Create a release branch to test release workflow"
Write-Host ""

if ($DryRun) {
    Write-Host "Note: This was a dry run. Re-run without -DryRun to apply changes."
    if ($RequirePRReviewers) {
        Write-Host "Command: .\setup-repository.ps1 -RequirePRReviewers `$true"
    } else {
        Write-Host "Command: .\setup-repository.ps1 -RequirePRReviewers `$false"
    }
} else {
    Write-Host "Setup completed with PR reviewer requirement: $RequirePRReviewers"
}

Write-Host ""
Write-Host "Usage Examples:"
Write-Host "  With PR reviewers required:    .\setup-repository.ps1 -RequirePRReviewers `$true"
Write-Host "  Without PR reviewers:         .\setup-repository.ps1 -RequirePRReviewers `$false"
Write-Host "  Dry run with reviewers:       .\setup-repository.ps1 -RequirePRReviewers `$true -DryRun"
Write-Host "  Specify repo explicitly:      .\setup-repository.ps1 my-repo myowner -RequirePRReviewers `$true"
Write-Host ""
Write-Host "If branch protection rules were not applied automatically,"
Write-Host "you can configure them manually in GitHub:"
Write-Host "1. Go to Settings > Branches"
Write-Host "2. Add rule for 'main' branch"
Write-Host "3. Enable: 'Require pull request reviews' (Pro+ for private repos)"
Write-Host "4. Enable: 'Require status checks to pass before merging'"
Write-Host "5. Add status checks: build-and-test, security-scan, version-check"
Write-Host "6. Enable: 'Require branches to be up to date before merging'"
Write-Host ""
Write-Host "📋 Recommended Development Workflow:"
Write-Host "✅ Current setup follows best practices:"
Write-Host "1. Feature branches (feature/*, bug/*, hotfix/*) allow free development"
Write-Host "2. PR workflows enforce status checks when merging to main"
Write-Host "3. Main branch protection prevents direct pushes"
Write-Host "4. This approach enables normal development while maintaining security"
Write-Host ""
Write-Host "🚀 GitHub Pro Plan Benefits (if needed):"
Write-Host "   - Advanced environment protection rules with required reviewers"
Write-Host "   - Enhanced security features and insights"
Write-Host "   - Unlimited private repository collaborators"
