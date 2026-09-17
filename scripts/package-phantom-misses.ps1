param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('linux-x64', 'win-x64')]
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Set-Location (Split-Path $PSScriptRoot -Parent)

if ($env:GITHUB_SHA -notmatch '^[0-9a-f]{40}$') {
    throw 'A full GITHUB_SHA is required to identify the packaged source.'
}

# Fail even if the test runner returned success without discovering the intended tests.
$reports = @{
    'phantom-misses.trx' = 23
    'mod-validity.trx' = 1
}
foreach ($name in $reports.Keys) {
    [xml] $report = Get-Content -LiteralPath "TestResults/$name" -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int] $counters.total -lt $reports[$name] -or
        [int] $counters.passed -ne [int] $counters.total -or
        [int] $counters.failed -ne 0) {
        throw "The required test suite did not pass completely: $name"
    }
    if ($name -eq 'phantom-misses.trx') {
        $classes = @($report.TestRun.TestDefinitions.UnitTest.TestMethod.className)
        foreach ($fixture in @('TestSceneOsuModPhantomMisses', 'TestSceneOsuModPhantomMissesSkins', 'TestSceneOsuModPhantomMissesMixed')) {
            if (-not ($classes | Where-Object { $_ -match "\.$fixture," -or $_ -match "\.$fixture`$" })) {
                throw "Required test fixture was not discovered: $fixture"
            }
        }
    }
    Write-Host "$name : $($counters.passed) passed, $($counters.failed) failed."
}

$folderName = "phantom-misses-$RuntimeIdentifier"
$package = Join-Path 'artifacts' $folderName
$executable = if ($RuntimeIdentifier -eq 'win-x64') { 'osu!.exe' } else { 'osu!' }
if (-not (Test-Path -LiteralPath (Join-Path $package $executable) -PathType Leaf)) {
    throw "Published executable is missing: $executable"
}

Copy-Item -LiteralPath 'LICENCE' -Destination $package
Copy-Item -LiteralPath 'PHANTOM_MISSES.md' -Destination (Join-Path $package 'README-PHANTOM-MISSES.md')
Copy-Item -LiteralPath 'PHANTOM_FIDELITY.md' -Destination $package
Copy-Item -LiteralPath 'PHANTOM_VERSION' -Destination $package
Set-Content -LiteralPath (Join-Path $package 'framework.ini') -Value 'WindowMode = Windowed' -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $package 'COMMIT.txt') -Value $env:GITHUB_SHA -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $package 'BUILD-CHANNEL.txt') -Value $env:GITHUB_REF -Encoding utf8NoBOM
$verification = New-Item -ItemType Directory -Path (Join-Path $package 'verification') -Force
foreach ($name in $reports.Keys) {
    Copy-Item -LiteralPath "TestResults/$name" -Destination $verification.FullName
}
New-Item -ItemType Directory -Path 'dist' -Force | Out-Null

if ($RuntimeIdentifier -eq 'win-x64') {
    # CRLF avoids command-shell parsing surprises regardless of checkout line-ending settings.
    $launcher = (Get-Content -LiteralPath 'scripts/run-phantom-misses.cmd' -Raw) -replace '\r?\n', "`r`n"
    [IO.File]::WriteAllText((Join-Path (Resolve-Path $package).Path 'run-phantom-misses.cmd'), $launcher, [Text.Encoding]::ASCII)
    $archive = "dist/$folderName.zip"
    Compress-Archive -LiteralPath $package -DestinationPath $archive -CompressionLevel Optimal
} else {
    & bash -n 'scripts/run-phantom-misses.sh'
    if ($LASTEXITCODE -ne 0) { throw 'Linux launcher syntax check failed.' }
    Copy-Item -LiteralPath 'scripts/run-phantom-misses.sh' -Destination $package
    & chmod +x (Join-Path $package 'osu!') (Join-Path $package 'run-phantom-misses.sh')
    if ($LASTEXITCODE -ne 0) { throw 'Could not preserve Linux executable permissions.' }
    $archive = "dist/$folderName.tar.gz"
    & tar -C artifacts -czf $archive $folderName
    if ($LASTEXITCODE -ne 0) { throw 'Linux archive creation failed.' }
}

$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
$archiveName = Split-Path $archive -Leaf
Set-Content -LiteralPath "$archive.sha256" -Value "$hash  $archiveName" -Encoding ascii
Write-Host "Packaged $archive ($((Get-Item -LiteralPath $archive).Length) bytes)"
