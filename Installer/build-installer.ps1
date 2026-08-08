<#
    Publiserer LMU Karriere som en self-contained single-file .exe og kompilerer den til en
    Windows-installer med Inno Setup. Kjør fra hvor som helst - stien regnes ut fra scriptet.

    Forutsetter Inno Setup 6 installert (gratis: https://jrsoftware.org/isdl.php).
    Alt annet (.NET 10 SDK, publisering) håndteres av dette scriptet.
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Repair-PublishContent {
    # dotnet publish -p:PublishSingleFile=true har vist seg å konsekvent hoppe over minst én
    # content-fil (myself.png - den klart største av portrettbildene, ~3.9 MB) i selve siste
    # kopieringssteget til publish-mappen, selv om filen ligger riktig i mellombygget
    # (bin\Release\...\win-x64\images\Drivers\). Dette er en kjent svakhet i
    # PublishSingleFile-pipelinen for større content-filer, ikke noe i dette prosjektets kode.
    # I stedet for å stole blindt på at "dotnet publish" returnerte 0, kopierer vi derfor selv
    # alt som mangler fra kildemappa etterpå.
    $sourceImages = "$repoRoot\LmuCareerTool.Core\images\Drivers"
    $targetImages = "publish\images\Drivers"
    New-Item -ItemType Directory -Force -Path $targetImages | Out-Null

    $missing = @()
    foreach ($src in Get-ChildItem $sourceImages -Filter "*.png") {
        $dst = Join-Path $targetImages $src.Name
        if (-not (Test-Path $dst)) {
            Copy-Item $src.FullName $dst
            $missing += $src.Name
        }
    }

    $contentSrc = "$repoRoot\LmuCareerTool.Core\Content\game-content.json"
    $contentDst = "publish\Content\game-content.json"
    if (-not (Test-Path $contentDst)) {
        New-Item -ItemType Directory -Force -Path "publish\Content" | Out-Null
        Copy-Item $contentSrc $contentDst
        $missing += "game-content.json"
    }

    if ($missing.Count -gt 0) {
        Write-Host "  Rettet opp $($missing.Count) fil(er) som publish-steget hoppet over: $($missing -join ', ')" -ForegroundColor Yellow
    }
}

Push-Location $repoRoot
try {
    Write-Host "[1/3] Publiserer self-contained single-file build..." -ForegroundColor Cyan
    if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }

    dotnet publish LmuCareerTool.App -c $Configuration -r win-x64 --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish feilet (kode $LASTEXITCODE)." }

    Repair-PublishContent

    if (-not (Test-Path "publish\LmuCareerTool.App.exe")) {
        throw "Fant ikke publish\LmuCareerTool.App.exe - publiseringen feilet på en måte reparasjonssteget ikke kan rette."
    }

    Write-Host "[2/3] Leter etter Inno Setup-kompilatoren (ISCC.exe)..." -ForegroundColor Cyan
    $isccPath = $null
    $onPath = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($onPath) {
        $isccPath = $onPath.Source
    }
    else {
        $candidates = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        )
        $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }

    if (-not $isccPath) {
        Write-Host ""
        Write-Host "Fant ikke Inno Setup. Publiseringen over er ferdig (se .\publish\)," -ForegroundColor Yellow
        Write-Host "men selve installer-kompileringen krever Inno Setup 6." -ForegroundColor Yellow
        Write-Host "Last det ned gratis her, og kjør dette scriptet på nytt:" -ForegroundColor Yellow
        Write-Host "  https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "[3/3] Kompilerer installer med Inno Setup..." -ForegroundColor Cyan
    & $isccPath "Installer\LmuCareerTool.iss"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup-kompilering feilet (kode $LASTEXITCODE)." }

    Write-Host ""
    Write-Host "Ferdig! Installeren ligger i Installer\Output\" -ForegroundColor Green
}
finally {
    Pop-Location
}
