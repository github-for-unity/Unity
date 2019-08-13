[CmdletBinding()]
Param(
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

$destdir = Join-Path $rootDirectory 'lib'
$destfile = Join-Path $destdir 'deps.zip'
Invoke-WebRequest -usebasicparsing "https://ghfvs-installer.github.com/dependencies/deps.zip" -OutFile $destfile
Push-Location $destdir
Run-Command -Fatal { &'7z' -y -bb3 x 'deps.zip' }
Pop-Location
