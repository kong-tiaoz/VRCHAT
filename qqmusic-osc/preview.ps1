$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'resolve-node.ps1')
& $taskNode (Join-Path $PSScriptRoot 'bridge.cjs') --preview
