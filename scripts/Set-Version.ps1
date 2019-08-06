Param(
	[Parameter(Mandatory=$true)]
	[int]
	$Value,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\modules.ps1 | out-null

$source = Get-Content -Raw $PSScriptRoot\TheVersion.cs
Add-Type -TypeDefinition "$source"

$versionFile = "$rootDirectory\src\com.github.ui\version.json"
$versionjson = Get-Content $versionFile | ConvertFrom-Json

$version = $versionjson.version

$parsed = [TheVersion]::Parse("$version")
Write-Output $parsed.Version
$newVersion = $parsed.SetPatch($Value)
Write-Output $newVersion.Version

$versionjson.version = $newVersion.Version
ConvertTo-Json $versionjson | Set-Content $versionFile
