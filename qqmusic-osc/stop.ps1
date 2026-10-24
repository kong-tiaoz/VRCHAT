$taskBridge = Join-Path $PSScriptRoot 'bridge.cjs'
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like "*$taskBridge*" } | ForEach-Object { Stop-Process -Id $_.ProcessId }
Write-Host 'Lyrics bridge stopped.'
