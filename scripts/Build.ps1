Param(
	[switch]
	$Trace = $false
)


Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

& {
	Trap {
		Write-Output "Build failed"
		Write-Output "Error: $_"
		exit -1
	}

	Run-Command -Fatal { .\hMSBuild.bat /t:restore /verbosity:minimal }
	Run-Command -Fatal { .\hMSBuild.bat /verbosity:minimal }

}