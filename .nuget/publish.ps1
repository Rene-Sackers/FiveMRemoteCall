$package = Get-ChildItem ".\packed\*.nupkg" | Sort-Object LastWriteTime | Select-Object -Last 1

Invoke-Expression "& .\nuget.exe push ""$package"" -src https://nuget.pkg.github.com/Rene-Sackers/index.json"