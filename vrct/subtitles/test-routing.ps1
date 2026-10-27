$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'configure-routing.ps1')
$fixtureDirectory = Join-Path $PSScriptRoot 'test-output\routing'
New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
$fixturePath = Join-Path $fixtureDirectory 'config.json'
$unicodeFixture = ([string][char]0x4F60) + ([string][char]0x597D)
$fixture = [ordered]@{WEBSOCKET_SERVER=$false;SEND_MESSAGE_TO_VRC=$true;SEND_RECEIVED_MESSAGE_TO_VRC=$true;WEBSOCKET_AUTH_TOKEN='test-local-only';SPEAKER_RECORD_TIMEOUT=1;SPEAKER_PHRASE_TIMEOUT=3;SPEAKER_MAX_PHRASES=10;SELECTED_TRANSLATION_COMPUTE_DEVICE=@{device='cuda';device_index=0};USER_TEST_TEXT=$unicodeFixture}
[IO.File]::WriteAllText($fixturePath, ($fixture | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
$changed = Set-VrctPrivateSubtitleRouting -ConfigPath $fixturePath
$actual = [IO.File]::ReadAllText($fixturePath) | ConvertFrom-Json
if (-not $changed -or -not $actual.WEBSOCKET_SERVER -or -not $actual.SEND_MESSAGE_TO_VRC -or $actual.SEND_RECEIVED_MESSAGE_TO_VRC) { throw 'Caption routing failed' }
if ($actual.WEBSOCKET_AUTH_TOKEN -ne 'test-local-only' -or $actual.SPEAKER_RECORD_TIMEOUT -ne 1 -or $actual.SPEAKER_PHRASE_TIMEOUT -ne 3 -or $actual.SPEAKER_MAX_PHRASES -ne 10 -or $actual.SELECTED_TRANSLATION_COMPUTE_DEVICE.device -ne 'cuda' -or $actual.USER_TEST_TEXT -ne $unicodeFixture) { throw 'Unrelated configuration changed' }
$before = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
$secondChange = Set-VrctPrivateSubtitleRouting -ConfigPath $fixturePath
$after = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
if ($secondChange -or $before -ne $after) { throw 'Routing setup is not idempotent' }
$original = [IO.File]::ReadAllText((Join-Path $fixtureDirectory 'config.before-private-subtitles.json')) | ConvertFrom-Json
if (-not $original.SEND_RECEIVED_MESSAGE_TO_VRC) { throw 'Original configuration backup is invalid' }
Write-Output 'PASS: private receive routing, outgoing chat preservation, settings preservation, backup and idempotence'
