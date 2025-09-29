# Version Management PowerShell Script
# This script handles semantic versioning operations for the CI/CD pipeline

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("get", "increment", "validate", "set-preview", "get-build")]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Component = "patch",
    
    [Parameter(Mandatory=$false)]
    [string]$PreviewSuffix = "",
    
    [Parameter(Mandatory=$false)]
    [string]$BuildNumber = "",
    
    [Parameter(Mandatory=$false)]
    [string]$VersionFilePath = "version.json"
)

# Function to read version from JSON file
function Get-VersionFromFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "Version file not found: $FilePath"
        exit 1
    }
    
    try {
        $versionData = Get-Content $FilePath -Raw | ConvertFrom-Json
        return $versionData
    }
    catch {
        Write-Error "Failed to parse version file: $_"
        exit 1
    }
}

# Function to write version to JSON file
function Set-VersionToFile {
    param(
        [string]$FilePath,
        [object]$VersionData
    )
    
    try {
        $VersionData.updated = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        $VersionData.updatedBy = $env:GITHUB_ACTOR ?? $env:USERNAME ?? "unknown"
        
        $jsonContent = $VersionData | ConvertTo-Json -Depth 10
        Set-Content -Path $FilePath -Value $jsonContent -Encoding UTF8
        
        Write-Host "Version file updated successfully"
    }
    catch {
        Write-Error "Failed to write version file: $_"
        exit 1
    }
}

# Function to parse semantic version
function Parse-SemanticVersion {
    param([string]$VersionString)
    
    if ($VersionString -match "^(\d+)\.(\d+)\.(\d+)$") {
        return @{
            Major = [int]$Matches[1]
            Minor = [int]$Matches[2]
            Patch = [int]$Matches[3]
        }
    }
    else {
        Write-Error "Invalid semantic version format: $VersionString"
        exit 1
    }
}

# Function to increment version component
function Increment-VersionComponent {
    param(
        [object]$VersionData,
        [string]$Component
    )
    
    $parsedVersion = Parse-SemanticVersion -VersionString $VersionData.version
    
    switch ($Component) {
        "major" {
            $parsedVersion.Major++
            $parsedVersion.Minor = 0
            $parsedVersion.Patch = 0
        }
        "minor" {
            $parsedVersion.Minor++
            $parsedVersion.Patch = 0
        }
        "patch" {
            $parsedVersion.Patch++
        }
    }
    
    $VersionData.version = "$($parsedVersion.Major).$($parsedVersion.Minor).$($parsedVersion.Patch)"
    $VersionData.prerelease = ""
    
    return $VersionData
}

# Function to create preview version
function Set-PreviewVersion {
    param(
        [object]$VersionData,
        [string]$PreviewSuffix
    )
    
    $VersionData.prerelease = $PreviewSuffix
    return $VersionData
}

# Function to get full version string
function Get-FullVersionString {
    param(
        [object]$VersionData,
        [string]$BuildNumber = ""
    )
    
    $version = $VersionData.version
    
    # Add build number if provided
    if ($BuildNumber) {
        $version += ".$BuildNumber"
    }
    elseif ($VersionData.build -and $VersionData.build -gt 0) {
        $version += ".$($VersionData.build)"
    }
    
    # Add prerelease suffix if present
    if ($VersionData.prerelease) {
        $version += "-$($VersionData.prerelease)"
    }
    
    return $version
}

# Function to validate version increment
function Test-VersionIncrement {
    param(
        [string]$CurrentVersion,
        [string]$NewVersion
    )
    
    $current = Parse-SemanticVersion -VersionString $CurrentVersion
    $new = Parse-SemanticVersion -VersionString $NewVersion
    
    # Check if version was incremented properly
    $majorIncremented = $new.Major -gt $current.Major
    $minorIncremented = $new.Minor -gt $current.Minor -and $new.Major -eq $current.Major
    $patchIncremented = $new.Patch -gt $current.Patch -and $new.Minor -eq $current.Minor -and $new.Major -eq $current.Major
    
    if ($majorIncremented -and $new.Minor -eq 0 -and $new.Patch -eq 0) {
        Write-Host "✓ Valid major version increment"
        return $true
    }
    elseif ($minorIncremented -and $new.Patch -eq 0) {
        Write-Host "✓ Valid minor version increment"
        return $true
    }
    elseif ($patchIncremented) {
        Write-Host "✓ Valid patch version increment"
        return $true
    }
    else {
        Write-Error "❌ Invalid version increment from $CurrentVersion to $NewVersion"
        Write-Host "Valid increments:"
        Write-Host "  Major: $($current.Major + 1).0.0"
        Write-Host "  Minor: $($current.Major).$($current.Minor + 1).0"
        Write-Host "  Patch: $($current.Major).$($current.Minor).$($current.Patch + 1)"
        return $false
    }
}

# Main script logic
try {
    switch ($Action) {
        "get" {
            $versionData = Get-VersionFromFile -FilePath $VersionFilePath
            $fullVersion = Get-FullVersionString -VersionData $versionData -BuildNumber $BuildNumber
            Write-Host $fullVersion
            
            # Set GitHub Actions outputs if running in GitHub Actions
            if ($env:GITHUB_OUTPUT) {
                "version=$($versionData.version)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
                "full-version=$fullVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
                "prerelease=$($versionData.prerelease)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
                "build=$($versionData.build)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            }
        }
        
        "increment" {
            $versionData = Get-VersionFromFile -FilePath $VersionFilePath
            $oldVersion = $versionData.version
            
            $versionData = Increment-VersionComponent -VersionData $versionData -Component $Component
            Set-VersionToFile -FilePath $VersionFilePath -VersionData $versionData
            
            Write-Host "Version incremented from $oldVersion to $($versionData.version)"
        }
        
        "validate" {
            # Get current version from main branch (would need git operations in real scenario)
            $versionData = Get-VersionFromFile -FilePath $VersionFilePath
            $currentVersion = $versionData.version
            
            # For now, we'll assume the new version is in the file
            # In a real PR scenario, we'd compare against main branch
            Write-Host "Current version: $currentVersion"
            Write-Host "✓ Version validation placeholder - implement git diff logic for PR validation"
        }
        
        "set-preview" {
            $versionData = Get-VersionFromFile -FilePath $VersionFilePath
            $versionData = Set-PreviewVersion -VersionData $versionData -PreviewSuffix $PreviewSuffix
            Set-VersionToFile -FilePath $VersionFilePath -VersionData $versionData
            
            $fullVersion = Get-FullVersionString -VersionData $versionData -BuildNumber $BuildNumber
            Write-Host "Preview version set: $fullVersion"
            
            if ($env:GITHUB_OUTPUT) {
                "preview-version=$fullVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            }
        }
        
        "get-build" {
            $versionData = Get-VersionFromFile -FilePath $VersionFilePath
            
            if ($BuildNumber) {
                $versionData.build = [int]$BuildNumber
                Set-VersionToFile -FilePath $VersionFilePath -VersionData $versionData
            }
            
            $fullVersion = Get-FullVersionString -VersionData $versionData -BuildNumber $BuildNumber
            Write-Host $fullVersion
            
            if ($env:GITHUB_OUTPUT) {
                "build-version=$fullVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
            }
        }
    }
}
catch {
    Write-Error "Script failed: $_"
    exit 1
}
