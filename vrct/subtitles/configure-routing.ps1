function Set-VrctPrivateSubtitleRouting {
    param([Parameter(Mandatory=$true)][string]$ConfigPath)
    $settings = [IO.File]::ReadAllText($ConfigPath) | ConvertFrom-Json
    if ($settings.WEBSOCKET_SERVER -and -not $settings.SEND_RECEIVED_MESSAGE_TO_VRC -and $settings.SEND_MESSAGE_TO_VRC) { return $false }
    $backup = Join-Path (Split-Path -Parent $ConfigPath) 'config.before-private-subtitles.json'
    if (-not (Test-Path -LiteralPath $backup)) { [IO.File]::Copy($ConfigPath, $backup) }
    $settings.WEBSOCKET_SERVER = $true
    $settings.SEND_MESSAGE_TO_VRC = $true
    $settings.SEND_RECEIVED_MESSAGE_TO_VRC = $false
    $temporary = $ConfigPath + '.private-subtitles.tmp'
    [IO.File]::WriteAllText($temporary, ($settings | ConvertTo-Json -Depth 40), [Text.UTF8Encoding]::new($false))
    [IO.File]::Replace($temporary, $ConfigPath, [System.Management.Automation.Language.NullString]::Value)
    return $true
}
