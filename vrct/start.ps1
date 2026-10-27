param([switch]$Cpu)

$ErrorActionPreference = 'Stop'
$cpuDirectory = Join-Path $PSScriptRoot 'app-cpu'
$appDirectory = if ($Cpu -and (Test-Path -LiteralPath (Join-Path $cpuDirectory 'VRCT.exe') -PathType Leaf)) {
    $cpuDirectory
} else {
    Join-Path $PSScriptRoot 'app'
}
$appExecutable = Join-Path $appDirectory 'VRCT.exe'
if (-not (Test-Path -LiteralPath $appExecutable -PathType Leaf)) {
    throw 'VRCT is missing. See README.md for the official download.'
}

# The lyrics bridge and translator share VRChat's one OSC chatbox.
# Stop only this workspace's sender, leaving QQ Music and its monitor running.
$lyricsBridge = Join-Path (Split-Path $PSScriptRoot) 'qqmusic-osc\bridge.cjs'
$nodeProcesses = Get-CimInstance Win32_Process -Filter "Name = 'node.exe'"
foreach ($nodeProcess in $nodeProcesses) {
    if ($nodeProcess.CommandLine -and
        $nodeProcess.CommandLine -match [regex]::Escape($lyricsBridge) -and
        $nodeProcess.CommandLine -notmatch '--preview') {
        Stop-Process -Id $nodeProcess.ProcessId
    }
}

$workspaceAppPaths = @((Join-Path $PSScriptRoot 'app\VRCT.exe'), (Join-Path $cpuDirectory 'VRCT.exe'))
$running = Get-Process -Name VRCT -ErrorAction SilentlyContinue | Where-Object { $_.Path -in $workspaceAppPaths }
if ($running -and ($running | Where-Object { $_.Path -ne $appExecutable })) {
    throw 'Close the currently running VRCT before switching between CPU and GPU editions.'
}
if (-not $running) {
    # Apply routing before VRCT starts, so its live configuration is never overwritten.
    $configPath = Join-Path $appDirectory 'config.json'
    if (Test-Path -LiteralPath $configPath -PathType Leaf) {
        . (Join-Path $PSScriptRoot 'subtitles\configure-routing.ps1')
        $null = Set-VrctPrivateSubtitleRouting -ConfigPath $configPath
    }
    Start-Process -FilePath $appExecutable -WorkingDirectory $appDirectory
}

$subtitlesRoot = Join-Path $PSScriptRoot 'subtitles'
$subtitleExecutable = Join-Path $subtitlesRoot 'bin\EnglishSubtitles.exe'
if (-not (Test-Path -LiteralPath $subtitleExecutable)) {
    & (Join-Path $subtitlesRoot 'build.ps1')
}
Start-Process -FilePath $subtitleExecutable -ArgumentList @('--config', ('"' + (Join-Path $appDirectory 'config.json') + '"')) -WorkingDirectory $subtitlesRoot -WindowStyle Normal
