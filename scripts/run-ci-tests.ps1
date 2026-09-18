param(
    [Parameter(Mandatory)]
    [ValidateSet('gameplay', 'editing', 'selection', 'navigation-main', 'navigation-other', 'online', 'other', 'rulesets')]
    [string] $Shard,
    [Parameter(Mandatory)]
    [string] $ReportName
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Ordered, exhaustive partition of osu.Game.Tests. A group excludes all earlier
# groups, and "other" receives everything left over, including newly added tests.
# Ruleset/template/tournament assemblies run in their entirety in "rulesets".
$groups = [ordered]@{
    'gameplay' = @('osu.Game.Tests.Visual.Gameplay.', 'osu.Game.Tests.Visual.Multiplayer.')
    'editing' = @('osu.Game.Tests.Visual.Editing.', 'osu.Game.Tests.Skins.', 'osu.Game.Tests.Scores.')
    'selection' = @('osu.Game.Tests.Visual.SongSelect.', 'osu.Game.Tests.Visual.UserInterface.')
    'navigation-main' = @('osu.Game.Tests.Visual.Navigation.TestSceneScreenNavigation', 'osu.Game.Tests.Visual.Navigation.TestSceneSkinEditorNavigation')
    'navigation-other' = @('osu.Game.Tests.Visual.Navigation.')
    'online' = @('osu.Game.Tests.Visual.Online.', 'osu.Game.Tests.Visual.Menus.', 'osu.Game.Tests.Visual.Matchmaking.', 'osu.Game.Tests.Visual.RankedPlay.', 'osu.Game.Tests.Visual.Playlists.', 'osu.Game.Tests.Visual.DailyChallenge.')
}

$clauses = [System.Collections.Generic.List[string]]::new()
if ($Shard -ne 'rulesets') {
    foreach ($entry in $groups.GetEnumerator()) {
        if ($entry.Key -eq $Shard) {
            $clauses.Add('(' + (($entry.Value | ForEach-Object { "FullyQualifiedName~$_" }) -join '|') + ')')
            break
        }
        foreach ($prefix in $entry.Value) {
            $clauses.Add("FullyQualifiedName!~$prefix")
        }
    }
}

$solution = Get-Content 'osu.Desktop.slnf' -Raw | ConvertFrom-Json
$assemblies = [System.Collections.Generic.List[string]]::new()
foreach ($project in $solution.solution.projects) {
    $projectPath = $project.Replace('\', '/')
    if (!$projectPath.EndsWith('.Tests.csproj')) { continue }
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
    if (($Shard -eq 'rulesets') -eq ($assemblyName -eq 'osu.Game.Tests')) { continue }

    $output = Join-Path ([System.IO.Path]::GetDirectoryName($projectPath)) 'bin/Debug'
    $builtAssemblies = @(Get-ChildItem $output -Directory | ForEach-Object {
        $candidate = Join-Path $_.FullName "$assemblyName.dll"
        if (Test-Path $candidate) { $candidate }
    })
    if ($builtAssemblies.Count -eq 0) { throw "No built test assembly for $projectPath" }
    foreach ($builtAssembly in $builtAssemblies) { $assemblies.Add($builtAssembly) }
}
if ($assemblies.Count -eq 0) { throw "No assemblies selected for shard $Shard" }

$arguments = @('test') + $assemblies.ToArray()
if ($clauses.Count -gt 0) { $arguments += @('--filter', ($clauses -join '&')) }
$arguments += @('--logger', "trx;LogFileName=$ReportName", '--results-directory', 'TestResults', '--', 'NUnit.ConsoleOut=0')
Write-Host "Running shard $Shard on $($assemblies.Count) test assemblies"
& dotnet @arguments
$testExitCode = $LASTEXITCODE
if ($testExitCode -ne 0) { exit $testExitCode }

# VSTest normally exits successfully for zero tests. Reject missing/empty reports
# so a malformed partition or missing assembly cannot produce a green CI job.
$reportPath = Join-Path 'TestResults' $ReportName
if (!(Test-Path $reportPath)) { throw "Missing report: $reportPath" }
[xml] $report = Get-Content $reportPath -Raw
$counters = $report.TestRun.ResultSummary.Counters
if ([int] $counters.executed -le 0) { throw "No tests executed in shard $Shard" }
if ([int] $counters.failed -gt 0) { throw "Failures reported in shard $Shard" }
Write-Host "Shard $Shard completed: $($counters.passed) passed, $($counters.failed) failed."
