CACLS ..\..\sitesroot /T /E /P "NETWORK SERVICE":F > networkservice_rights.txt
rem CACLS ..\..\sitesroot /T /E /P IUSR:F > iusr_rights.txt

%SYSTEMROOT%\System32\inetsrv\appcmd.exe set config -section:system.webServer/httpCompression /[name='gzip'].dynamicCompressionLevel:"9" /commit:apphost
%SYSTEMROOT%\system32\inetsrv\appcmd.exe set config -section:applicationPools -applicationPoolDefaults.managedRuntimeVersion:v4.0

del /f /q "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\*"
for /d %%p in ("%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\*") do rmdir "%%p" /s /q