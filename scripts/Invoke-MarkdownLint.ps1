<#
.SYNOPSIS
    Runs markdownlint on all markdown files in the repository.

.DESCRIPTION
    This script checks for Node.js and npm, installs markdownlint-cli if needed,
    and runs markdown linting with optional auto-fix capability.

.PARAMETER Fix
    Automatically fix markdown linting issues where possible.

.PARAMETER FilePath
    Specific file or pattern to lint. Defaults to all markdown files.

.PARAMETER ConfigPath
    Path to markdownlint configuration file. Defaults to .github/linters/.markdownlint.json

.EXAMPLE
    .\Invoke-MarkdownLint.ps1
    Runs linting in check-only mode.

.EXAMPLE
    .\Invoke-MarkdownLint.ps1 -Fix
    Runs linting and automatically fixes issues.

.EXAMPLE
    .\Invoke-MarkdownLint.ps1 -FilePath "README.md"
    Lints only README.md file.

.NOTES
    Requires Node.js and npm to be installed.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [switch]$Fix,

    [Parameter(Mandatory = $false)]
    [string]$FilePath = "**/*.md",

    [Parameter(Mandatory = $false)]
    [string]$ConfigPath = ".github/linters/.markdownlint.json"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get repository root (script is in scripts/ folder)
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "  Markdown Linter Setup & Execution" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""

    # Check for Node.js
    Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
    try {
        $nodeVersion = node --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Node.js installed: $nodeVersion" -ForegroundColor Green
        } else {
            throw "Node.js not found"
        }
    } catch {
        Write-Host "✗ Node.js is not installed or not in PATH" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install Node.js from: https://nodejs.org/" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }

    # Check for npm
    Write-Host "Checking npm installation..." -ForegroundColor Yellow
    try {
        $npmVersion = npm --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ npm installed: v$npmVersion" -ForegroundColor Green
        } else {
            throw "npm not found"
        }
    } catch {
        Write-Host "✗ npm is not installed or not in PATH" -ForegroundColor Red
        Write-Host ""
        Write-Host "npm should be installed with Node.js. Please reinstall Node.js." -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }

    Write-Host ""

    # Check for markdownlint-cli
    Write-Host "Checking markdownlint-cli installation..." -ForegroundColor Yellow
    $markdownlintInstalled = $false
    try {
        $mlVersion = markdownlint --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ markdownlint-cli installed: v$mlVersion" -ForegroundColor Green
            $markdownlintInstalled = $true
        }
    } catch {
        # Not installed, will install below
    }

    if (-not $markdownlintInstalled) {
        Write-Host "✗ markdownlint-cli not found" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Installing markdownlint-cli globally..." -ForegroundColor Cyan
        try {
            npm install -g markdownlint-cli
            if ($LASTEXITCODE -ne 0) {
                throw "npm install failed"
            }
            $mlVersion = markdownlint --version 2>$null
            Write-Host "✓ markdownlint-cli installed: v$mlVersion" -ForegroundColor Green
        } catch {
            Write-Host "✗ Failed to install markdownlint-cli" -ForegroundColor Red
            Write-Host ""
            Write-Host "Try running manually: npm install -g markdownlint-cli" -ForegroundColor Yellow
            Write-Host ""
            exit 1
        }
    }

    Write-Host ""

    # Verify config file exists
    if (-not (Test-Path $ConfigPath)) {
        Write-Host "✗ Configuration file not found: $ConfigPath" -ForegroundColor Red
        Write-Host ""
        exit 1
    }

    Write-Host "✓ Configuration file found: $ConfigPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""

    # Build command arguments
    $lintArgs = @(
        "--config", $ConfigPath,
        $FilePath,
        "--ignore", "node_modules"
    )

    if ($Fix) {
        Write-Host "Running markdownlint with AUTO-FIX enabled..." -ForegroundColor Cyan
        $lintArgs += "--fix"
    } else {
        Write-Host "Running markdownlint in CHECK-ONLY mode..." -ForegroundColor Cyan
    }

    Write-Host "Command: markdownlint $($lintArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""

    # Run markdownlint
    & markdownlint @lintArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host "✓ Markdown linting completed successfully!" -ForegroundColor Green
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host ""

        if ($Fix) {
            Write-Host "Files have been automatically fixed." -ForegroundColor Green
            Write-Host "Review changes with: git diff" -ForegroundColor Yellow
        } else {
            Write-Host "No issues found! All markdown files are compliant." -ForegroundColor Green
        }

        exit 0
    } else {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Yellow
        Write-Host "⚠ Markdown linting found issues" -ForegroundColor Yellow
        Write-Host "=====================================" -ForegroundColor Yellow
        Write-Host ""

        if (-not $Fix) {
            Write-Host "To automatically fix issues, run:" -ForegroundColor Cyan
            Write-Host "  .\scripts\Invoke-MarkdownLint.ps1 -Fix" -ForegroundColor White
            Write-Host ""
            Write-Host "Or to fix a specific file:" -ForegroundColor Cyan
            Write-Host "  .\scripts\Invoke-MarkdownLint.ps1 -Fix -FilePath 'README.md'" -ForegroundColor White
        } else {
            Write-Host "Some issues could not be auto-fixed." -ForegroundColor Yellow
            Write-Host "Please review the output above and fix manually." -ForegroundColor Yellow
        }

        Write-Host ""
        Write-Host "For detailed report, see: docs/MARKDOWN-LINT-REPORT.md" -ForegroundColor Cyan
        Write-Host ""

        exit 1
    }

} catch {
    Write-Host ""
    Write-Host "✗ An error occurred: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
} finally {
    Pop-Location
}
