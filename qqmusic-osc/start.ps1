$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskBridge = Join-Path $taskRoot 'bridge.cjs'
$taskMonitor = Join-Path $taskRoot 'monitor\qqmusic-monitor.exe'
. (Join-Path $taskRoot 'resolve-node.ps1')
if ($env:QQMUSIC_MONITOR_EXE) { $taskMonitor = $env:QQMUSIC_MONITOR_EXE }
if (-not (Test-Path -LiteralPath $taskMonitor -PathType Leaf)) { throw 'Install QQMusicMonitor into monitor, or set QQMUSIC_MONITOR_EXE to its executable path.' }
$taskProcesses = Get-CimInstance Win32_Process
if (-not ($taskProcesses | Where-Object { $_.ExecutablePath -eq $taskMonitor })) {
    Start-Process -FilePath $taskMonitor -WorkingDirectory (Split-Path $taskMonitor) -WindowStyle Hidden
}
if (-not ($taskProcesses | Where-Object { $_.Name -eq 'node.exe' -and $_.CommandLine -like "*$taskBridge*" })) {
    Start-Process -FilePath $taskNode -ArgumentList ('"' + $taskBridge + '"') -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskRoot 'bridge.log') -RedirectStandardError (Join-Path $taskRoot 'bridge-error.log')
}
Write-Host 'QQMusic lyrics bridge started. Enable OSC inside VRChat.'
