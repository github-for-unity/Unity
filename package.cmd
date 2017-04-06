set GITHUB_UNITY_DISABLE=1
%1\Unity.exe -batchmode -projectPath %~dp0unity\PackageProject -exportPackage Assets\Editor\GitHub github-for-unity-windows.unitypackage -force-free -quit
