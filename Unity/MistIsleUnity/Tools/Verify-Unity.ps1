param(
    [string]$EditorPath = 'E:\unityHubinstall\6000.3.28f1\Editor\Unity.exe',
    [switch]$BuildWindows
)
$ErrorActionPreference = 'Stop'
$sourceProject = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$verificationProject = Join-Path ([IO.Path]::GetTempPath()) ('MistIsle-Verify-' + [Guid]::NewGuid().ToString('N'))
$outputDirectory = Join-Path $sourceProject ('Builds\Verification\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $verificationProject,$outputDirectory -Force | Out-Null
foreach ($name in @('Assets','Packages','ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $sourceProject $name) -Destination $verificationProject -Recurse -Force
}
function Invoke-UnityStage([string]$name, [string[]]$stageArguments) {
    $log = Join-Path $outputDirectory ($name + '.log')
    $arguments = @('-batchmode','-projectPath',('"' + $verificationProject + '"'),'-logFile',('"' + $log + '"')) + $stageArguments
    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $log -Tail 30; throw "$name failed ($($process.ExitCode)); log: $log" }
    Write-Output "$name passed: $log"
}
Invoke-UnityStage 'content' @('-nographics','-quit','-executeMethod','MistIsle.Editor.MistIsleProjectBuilder.BuildContent')
Invoke-UnityStage 'editmode' @('-nographics','-runTests','-testPlatform','EditMode','-testResults',('"' + (Join-Path $outputDirectory 'editmode.xml') + '"'))
$env:MIST_ISLE_CAPTURE_DIR = Join-Path $outputDirectory 'Captures'
Invoke-UnityStage 'playmode' @('-runTests','-testPlatform','PlayMode','-testResults',('"' + (Join-Path $outputDirectory 'playmode.xml') + '"'))
if ($BuildWindows) {
    $env:MIST_ISLE_BUILD_PATH = Join-Path $sourceProject 'Builds\Windows\MistIsle.exe'
    Invoke-UnityStage 'windows' @('-nographics','-quit','-executeMethod','MistIsle.Editor.MistIsleProjectBuilder.BuildWindows')
}
Write-Output "Evidence: $outputDirectory"
Write-Output "Verification project retained for inspection: $verificationProject"
