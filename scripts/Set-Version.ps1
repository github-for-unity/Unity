Param(
	[string]
	$NewVersion = ''
	,
	[switch]
	$BumpMajor = $false
	,
	[switch]
	$BumpMinor = $false
	,
	[switch]
	$BumpPatch = $false
	,
	[switch]
	$BumpBuild = $false
	,
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

	if ($NewVersion -eq '') {
		if (!$BumpMajor -and !$BumpMinor -and !$BumpPatch -and !$BumpBuild){
			Die -1 "You need to indicate which part of the version to update via -BumpMajor/-BumpMinor/-BumpPatch/-BumpBuild flags or a custom version via -NewVersion"
		}
	}

	$source = Get-Content -Raw $PSScriptRoot\TheVersion.cs
	Add-Type -TypeDefinition "$source"

	function Read-Version([string]$versionFile) {
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")
		$parsed
	}

	function Write-Version([string]$versionFile, [TheVersion]$version) {
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$versionjson.version = $version.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	function Set-Version([string]$versionFile, [string]$newValue) {
		$parsed = [TheVersion]::Parse("$newValue")
		Write-Version $versionFile $parsed
	}

	function Bump-Version([string]$versionFile,
		[bool]$bumpMajor, [bool] $bumpMinor,
		[bool]$bumpPatch, [bool] $bumpBuild,
		[string]$newValue = '')
	{
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")

		if ($bumpMajor) {
			if ($newValue -ne '') {
				$newVersion = $parsed.SetMajor($newValue)
			} else {
				$newVersion = $parsed.bumpMajor()
			}
		} elseif ($bumpMinor) {
			if ($newValue -ne '') {
				$newVersion = $parsed.SetMinor($newValue)
			} else {
				$newVersion = $parsed.BumpMinor()
			}
		} elseif ($bumpPatch) {
			if ($newValue -ne '') {
				$newVersion = $parsed.SetPatch($newValue)
			} else {
				$newVersion = $parsed.BumpPatch()
			}
		} elseif ($bumpBuild) {
			if ($newValue -ne '') {
				$newVersion = $parsed.SetBuild($newValue)
			} else {
				$newVersion = $parsed.BumpBuild()
			}
		}

		$versionjson.version = $newVersion.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	if ($NewVersion -ne '' -and !($BumpMajor -or $BumpMinor -or $BumpPatch -or $BumpBuild)) {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Set-Version $versionFile $NewVersion

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Set-Version $versionFile $NewVersion
	} else {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $NewVersion

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $NewVersion
	}

}