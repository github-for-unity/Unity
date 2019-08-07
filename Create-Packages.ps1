<#
.SYNOPSIS
    Packages a build of GitHub for Unity
.DESCRIPTION
    Packages a build of GitHub for Unity
#>

[CmdletBinding()]

Param(
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\scripts\modules.ps1 | out-null

$uiVersionData = %{ & "$rootDirectory\packages\Nerdbank.GitVersioning.3.0.24\tools\Get-Version.ps1" -ProjectDirectory "$rootDirectory\src\com.github.ui\UI\" }
$apiVersionData = %{ & "$rootDirectory\packages\Nerdbank.GitVersioning.3.0.24\tools\Get-Version.ps1" -ProjectDirectory "$rootDirectory\src\git-for-unity\src\com.unity.git.api\Api\" }
$uiVersion = $uiVersionData.AssemblyInformationalVersion
$apiVersion = $apiVersionData.AssemblyInformationalVersion

$PackageName = "github-for-unity"

$name1 = "com.github.ui"
$path1 = "$rootDirectory\build\packages\$name1"
$extras1 = "$rootDirectory\src\extras\$name1"
$ignores1 = "$rootDirectory\build\packages\$name1\.npmignore"
$version1 = "$uiVersion"

$name2 = "com.unity.git.api"
$path2 = "$rootDirectory\build\packages\$name2"
$extras2 = "$rootDirectory\src\git-for-unity\src\extras\$name2"
$ignores2 = "$rootDirectory\build\packages\$name2\.npmignore" 
$version2 = "$apiVersion"

Run-Command -Fatal { & "$rootDirectory\submodules\packaging\unitypackage\run.ps1" -PathToPackage "$rootDirectory\unity\GHfU-net35" -OutputFolder "$rootDirectory" -PackageName "$PackageName-net20-$uiVersion" }
Run-Command -Fatal { & "$rootDirectory\submodules\packaging\unitypackage\run.ps1" -PathToPackage "$rootDirectory\unity\GHfU-net471" -OutputFolder "$rootDirectory" -PackageName "$PackageName-$uiVersion" }

Run-Command -Fatal { & "$rootDirectory\src\git-for-unity\packaging\create-unity-packages\run.ps1" -PathToPackage "$path1" -OutputFolder "$rootDirectory" -PackageName "$name1" -Version "$version1" -Ignores "$ignores1" }
Run-Command -Fatal { & "$rootDirectory\src\git-for-unity\packaging\create-unity-packages\run.ps1" -PathToPackage "$path2" -OutputFolder "$rootDirectory" -PackageName "$name2" -Version "$version2" -Ignores "$ignores2" }

Run-Command -Fatal { & "$rootDirectory\src\git-for-unity\packaging\create-unity-packages\multipackage.ps1" -OutputFolder "$rootDirectory" -PackageName "$PackageName-packman" -Version "$uiVersion" -Path1 "$path1" -Extras1 "$extras1"  -Ignores1 "$ignores1" -Path2 "$path2" -Extras2 "$extras2"  -Ignores2 "$ignores2" }
