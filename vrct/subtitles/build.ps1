$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$output = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$references = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$sources = @('CaptionProtocol.cs','CaptionClient.cs','SubtitleWindow.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 ('/out:' + (Join-Path $output 'EnglishSubtitles.exe')) $references $sources
if ($LASTEXITCODE -ne 0) { throw 'Subtitle window compilation failed.' }
& $compiler /nologo /target:exe /platform:x64 /optimize+ /codepage:65001 ('/out:' + (Join-Path $output 'SubtitleTests.exe')) $references (Join-Path $PSScriptRoot 'CaptionProtocol.cs') (Join-Path $PSScriptRoot 'CaptionClient.cs') (Join-Path $PSScriptRoot 'Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Subtitle tests compilation failed.' }
Write-Output 'Built EnglishSubtitles.exe using the installed Windows .NET Framework.'
