# Build script for ModernEntityDesigner
$ErrorActionPreference = "Stop"

# Find vswhere
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    Write-Error "Could not find vswhere.exe at $vswhere"
    exit 1
}

# Find latest VS installation (including prerelease/insiders)
$vsPath = & $vswhere -latest -prerelease -property installationPath
if (-not $vsPath) {
    Write-Error "Could not find Visual Studio installation"
    exit 1
}

Write-Host "Found Visual Studio at: $vsPath" -ForegroundColor Cyan

# Import VS Developer environment
$vsDevCmd = Join-Path $vsPath "Common7\Tools\Launch-VsDevShell.ps1"
if (Test-Path $vsDevCmd) {
    & $vsDevCmd -SkipAutomaticLocation
} else {
    # Fallback to batch file method
    $batchFile = Join-Path $vsPath "Common7\Tools\VsDevCmd.bat"
    cmd /c "`"$batchFile`" && set" | ForEach-Object {
        if ($_ -match "^([^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
}

# Change to repo directory
Push-Location D:\GitHub\ef6tools

try {
    # Build
    Write-Host "`nBuilding ModernEntityDesigner.slnx..." -ForegroundColor Green
    dotnet build ModernEntityDesigner.slnx -c Debug

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host "`nBuild completed successfully!" -ForegroundColor Green
} finally {
    Pop-Location
}
