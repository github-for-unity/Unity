[CmdletBinding()]
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

	Write-Verbose "Set-Version: NewVersion: $NewVersion BumpMajor: $BumpMajor BumpMinor: $BumpMinor BumpPatch: $BumpPatch BumpBuild: $BumpBuild"

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

	function Write-Version([string]$versionFile, [TheVersion]$version, [bool]$verbose) {
		Write-Verbose "Writing version $version to $versionFile "
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$versionjson.version = $version.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	function Set-Version([string]$versionFile, [string]$newValue, [bool]$verbose) {
		$parsed = [TheVersion]::Parse("$newValue")
		Write-Version $versionFile $parsed $verbose
	}

	function Bump-Version([string]$versionFile,
		[bool]$bumpMajor, [bool] $bumpMinor,
		[bool]$bumpPatch, [bool] $bumpBuild,
		[string]$newValue,
		[bool]$verbose)
	{
		Write-Verbose "Reading $versionFile"

		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")

		Write-Verbose "Read version $parsed"

		if ($bumpMajor) {
			if ($newValue -ne '') {
				Write-Verbose "Setting major part $newValue"
				$newVersion = $parsed.SetMajor($newValue)
			} else {
				Write-Verbose "Bumping major part"
				$newVersion = $parsed.bumpMajor()
			}
		} elseif ($bumpMinor) {
			if ($newValue -ne '') {
				Write-Verbose "Setting minor part $newValue"
				$newVersion = $parsed.SetMinor($newValue)
			} else {
				Write-Verbose "Bumping minor part"
				$newVersion = $parsed.BumpMinor()
			}
		} elseif ($bumpPatch) {
			if ($newValue -ne '') {
				Write-Verbose "Setting patch part $newValue"
				$newVersion = $parsed.SetPatch($newValue)
			} else {
				Write-Verbose "Bumping patch part"
				$newVersion = $parsed.BumpPatch()
			}
		} elseif ($bumpBuild) {
			if ($newValue -ne '') {
				Write-Verbose "Setting build part $newValue"
				$newVersion = $parsed.SetBuild($newValue)
			} else {
				Write-Verbose "Bumping build part"
				$newVersion = $parsed.BumpBuild()
			}
		}

		Write-Verbose "Writing version $($newVersion.Version) to $versionFile "
		$versionjson.version = $newVersion.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	if ($NewVersion -ne '' -and !($BumpMajor -or $BumpMinor -or $BumpPatch -or $BumpBuild)) {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Set-Version $versionFile $NewVersion $Verbose

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Set-Version $versionFile $NewVersion $Verbose
	} else {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $NewVersion $Verbose

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $NewVersion $Verbose
	}
}