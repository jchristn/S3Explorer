<#
.SYNOPSIS
    Build script for S3 Explorer - publishes the app, creates an installer, and packs the Chocolatey package.

.PARAMETER Version
    The version number to use (e.g. "1.0.0"). Updates the Inno Setup script and nuspec.

.PARAMETER SkipPublish
    Skip the dotnet publish step (use existing publish output).

.PARAMETER SkipInstaller
    Skip the Inno Setup installer build step.

.PARAMETER SkipChoco
    Skip the Chocolatey package build step.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Version "1.2.0"
    .\build.ps1 -SkipPublish
#>

param(
    [string]$Version = "1.0.0",
    [switch]$SkipPublish,
    [switch]$SkipInstaller,
    [switch]$SkipChoco
)

$ErrorActionPreference = 'Stop'
$rootDir = $PSScriptRoot
$srcDir = Join-Path $rootDir "src\S3Explorer"
$publishDir = Join-Path $rootDir "publish\win-x64"
$installerDir = Join-Path $rootDir "publish\installer"
$chocoDir = Join-Path $rootDir "chocolatey"
$issFile = Join-Path $rootDir "installer\s3explorer.iss"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  S3 Explorer Build Script v$Version" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: dotnet publish
if (-not $SkipPublish) {
    Write-Host "[1/3] Publishing .NET application..." -ForegroundColor Yellow

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    dotnet publish $srcDir -c Release -r win-x64 --self-contained -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
        exit 1
    }

    Write-Host "Published to: $publishDir" -ForegroundColor Green
} else {
    Write-Host "[1/3] Skipping dotnet publish (--SkipPublish)" -ForegroundColor DarkGray
}

# Step 2: Inno Setup installer
if (-not $SkipInstaller) {
    Write-Host "[2/3] Building Inno Setup installer..." -ForegroundColor Yellow

    # Find Inno Setup compiler
    $iscc = $null
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            $iscc = $candidate
            break
        }
    }

    if (-not $iscc) {
        Write-Warning "Inno Setup 6 not found. Install from https://jrsoftware.org/isdl.php"
        Write-Warning "Skipping installer build."
    } else {
        if (-not (Test-Path $installerDir)) {
            New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
        }

        & $iscc "/DMyAppVersion=$Version" $issFile
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
            exit 1
        }

        $installerExe = Join-Path $installerDir "S3ExplorerSetup-$Version.exe"
        if (Test-Path $installerExe) {
            $hash = (Get-FileHash $installerExe -Algorithm SHA256).Hash
            Write-Host "Installer: $installerExe" -ForegroundColor Green
            Write-Host "SHA256:    $hash" -ForegroundColor Green
            Write-Host ""
            Write-Host "Use this checksum in chocolateyinstall.ps1" -ForegroundColor Magenta
        }
    }
} else {
    Write-Host "[2/3] Skipping Inno Setup (--SkipInstaller)" -ForegroundColor DarkGray
}

# Step 3: Chocolatey package
if (-not $SkipChoco) {
    Write-Host "[3/3] Building Chocolatey package..." -ForegroundColor Yellow

    $chocoExe = Get-Command choco -ErrorAction SilentlyContinue
    if (-not $chocoExe) {
        Write-Warning "Chocolatey not found. Install from https://chocolatey.org/install"
        Write-Warning "Skipping Chocolatey package build."
    } else {
        # Update version in nuspec
        $nuspecFile = Join-Path $chocoDir "s3explorer.nuspec"
        $nuspecContent = Get-Content $nuspecFile -Raw
        $nuspecContent = $nuspecContent -replace '<version>[^<]+</version>', "<version>$Version</version>"
        Set-Content $nuspecFile -Value $nuspecContent -NoNewline

        Push-Location $chocoDir
        try {
            choco pack
            if ($LASTEXITCODE -ne 0) {
                Write-Error "choco pack failed with exit code $LASTEXITCODE"
                exit 1
            }
        } finally {
            Pop-Location
        }

        $nupkg = Get-ChildItem -Path $chocoDir -Filter "s3explorer.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($nupkg) {
            Write-Host "Chocolatey package: $($nupkg.FullName)" -ForegroundColor Green
        }
    }
} else {
    Write-Host "[3/3] Skipping Chocolatey pack (--SkipChoco)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Build complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Upload the installer to a GitHub Release"
Write-Host "  2. Update chocolatey\tools\chocolateyinstall.ps1 with the download URL and SHA256 checksum"
Write-Host "  3. Run 'choco push' to publish to the Chocolatey community repository"
Write-Host ""
