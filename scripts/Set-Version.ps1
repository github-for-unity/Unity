Param(
	[Parameter(Mandatory=$true)]
	[int]
	$PatchVersion,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

& {
    Trap {
        Write-Output "Setting version failed"
        Write-Output "Error: $_"
        exit 0
    }

	$source = Get-Content -Raw $PSScriptRoot\TheVersion.cs
	Add-Type -TypeDefinition "$source"

	function PatchVersionFile([string]$versionFile, [int]$newPatchValue) {
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")
		$newVersion = $parsed.SetPatch($newPatchValue)
		$versionjson.version = $newVersion.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	$versionFile = "$rootDirectory\src\com.github.ui\version.json"
	PatchVersionFile $versionFile $PatchVersion

	$versionFile = "$rootDirectory\src\git-for-unity\src\com.unity.git.ui\version.json"
	PatchVersionFile $versionFile $PatchVersion

	$versionFile = "$rootDirectory\src\git-for-unity\src\com.unity.git.api\version.json"
	PatchVersionFile $versionFile $PatchVersion

}
