# Test script to demonstrate version.json handling
# This script shows how the version management handles missing files

Write-Host "=== Testing Version Management with Missing Files ==="
Write-Host ""

# Save current version.json if it exists
$originalExists = Test-Path "version.json"
$originalContent = $null
if ($originalExists) {
    $originalContent = Get-Content "version.json" -Raw
    Write-Host "Backing up existing version.json"
}

try {
    # Test 1: Remove version.json and test get-build action
    Write-Host "Test 1: Testing with missing version.json file"
    if (Test-Path "version.json") {
        Remove-Item "version.json" -Force
    }
    
    Write-Host "Running: version-management.ps1 -Action get-build -BuildNumber 123"
    & ".\version-management.ps1" -Action get-build -BuildNumber 123
    
    Write-Host "Generated version.json content:"
    if (Test-Path "version.json") {
        Get-Content "version.json" | Write-Host
    } else {
        Write-Host "No version.json was created!"
    }
    
    Write-Host ""
    Write-Host "Test 2: Testing safe version info function"
    
    # Test the safe version function directly
    . ".\version-management.ps1"
    $safeVersion = Get-SafeVersionInfo
    Write-Host "Safe version info: $($safeVersion | ConvertTo-Json)"
    
} finally {
    # Restore original version.json if it existed
    if ($originalExists -and $originalContent) {
        Set-Content "version.json" -Value $originalContent
        Write-Host ""
        Write-Host "Restored original version.json"
    } elseif (-not $originalExists -and (Test-Path "version.json")) {
        Remove-Item "version.json" -Force
        Write-Host ""
        Write-Host "Cleaned up test version.json"
    }
}

Write-Host ""
Write-Host "=== Test Complete ==="