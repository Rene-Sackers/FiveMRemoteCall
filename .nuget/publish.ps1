$packages = Get-ChildItem ".\packed\*.nupkg"

foreach ($package in $packages) {
	Invoke-Expression "& .\nuget.exe push ""$package"" -src https://nuget.pkg.github.com/Rene-Sackers/index.json"
}