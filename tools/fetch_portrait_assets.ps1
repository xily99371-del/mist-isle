$ErrorActionPreference = "Stop"
$repo = "xily99371-del/mist-isle"
$required = @("heroine_portrait.png", "watcher_portrait.png", "lighthouse_guardian_portrait.png")
$target = Join-Path (Get-Location) "assets/characters/png"
New-Item -ItemType Directory -Force -Path $target | Out-Null
if (($required | Where-Object { -not (Test-Path (Join-Path $target $_)) }).Count -eq 0) {
    Write-Host "Portrait assets already present."
    exit 0
}
$headers = @{ "User-Agent" = "MistIsle-Windows-Setup"; "Accept" = "application/vnd.github+json" }
$api = "https://api.github.com/repos/$repo/actions/artifacts?per_page=100"
try {
    $response = Invoke-RestMethod -Uri $api -Headers $headers -Method Get
} catch {
    throw "Could not query the public Mist Isle build artifacts. Check your network connection."
}
$artifact = $response.artifacts |
    Where-Object { $_.name -eq "MistIsle-RaspberryPi5-ARM64" -and -not $_.expired } |
    Sort-Object created_at -Descending |
    Select-Object -First 1
if (-not $artifact) {
    throw "No Raspberry Pi art-build artifact is available yet. Wait for the GitHub Actions Linux build to pass, then run again."
}
$temp = Join-Path $env:TEMP ("mist-isle-portraits-" + [guid]::NewGuid().ToString("N"))
$zip = "$temp.zip"
New-Item -ItemType Directory -Force -Path $temp | Out-Null
try {
    Invoke-WebRequest -Uri $artifact.archive_download_url -Headers $headers -OutFile $zip
    Expand-Archive -LiteralPath $zip -DestinationPath $temp -Force
    $source = Join-Path $temp "assets/characters/png"
    foreach ($name in $required) {
        $file = Join-Path $source $name
        if (-not (Test-Path $file)) { throw "Portrait asset missing from build artifact: $name" }
        Copy-Item -LiteralPath $file -Destination (Join-Path $target $name) -Force
    }
    Write-Host "Installed the three high-resolution dialogue portraits."
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
}
