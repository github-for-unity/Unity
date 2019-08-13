@echo off
setlocal

set Configuration=dev
if not %1.==. (
	set Configuration=%1
)

set Target=Build
if not %2.==. (
	set Target=%2
)

call hMSBuild.bat /t:restore
call hMSBuild.bat /verbosity:minimal /property:Configuration=%Configuration% /target:%Target%
