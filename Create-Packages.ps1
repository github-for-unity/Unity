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

Run-Command -Fatal { & "$rootDirectory\submodules\packaging\unitypackage\run.ps1" -PathToPackage "$rootDirectory\unity\GHfU-net35" -OutputFolder "$rootDirectory" -PackageName "github-for-unity-net20-$uiVersion" }
Run-Command -Fatal { & "$rootDirectory\submodules\packaging\unitypackage\run.ps1" -PathToPackage "$rootDirectory\unity\GHfU-net471" -OutputFolder "$rootDirectory" -PackageName "github-for-unity-$uiVersion" }

Run-Command -Fatal { & "$rootDirectory\src\git-for-unity\packaging\create-unity-packages\run.ps1" -PathToPackage "$rootDirectory\packages\com.github.ui" -OutputFolder "$rootDirectory" -PackageName com.github.ui -Version "$uiVersion" -Ignores "$rootDirectory\packages\com.github.ui\.npmignore" }
Run-Command -Fatal { & "$rootDirectory\src\git-for-unity\packaging\create-unity-packages\run.ps1" -PathToPackage "$rootDirectory\packages\com.unity.git.api" -OutputFolder "$rootDirectory" -PackageName com.unity.git.api -Version "$apiVersion" -Ignores "$rootDirectory\packages\com.unity.git.api\.npmignore" }

