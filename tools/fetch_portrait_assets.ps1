$ErrorActionPreference = "Stop"
Write-Host "Portrait download is no longer required for a local Windows build."
Write-Host "The source checkout uses the approved pixel sprite fallbacks."
Write-Host "The packaged Windows build receives the high-resolution portraits from CI."
exit 0
