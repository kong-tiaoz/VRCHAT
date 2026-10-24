if ($env:QQMUSIC_OSC_NODE) {
    if (-not (Test-Path -LiteralPath $env:QQMUSIC_OSC_NODE -PathType Leaf)) { throw 'QQMUSIC_OSC_NODE does not point to node.exe.' }
    $taskNode = $env:QQMUSIC_OSC_NODE
} else {
    $taskNode = (Get-Command node.exe -ErrorAction Stop).Source
}
