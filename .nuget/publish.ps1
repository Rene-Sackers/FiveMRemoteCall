$packages = Get-ChildItem ".\packed\*.nupkg"

$confirmation = Read-Host "Publish packages? (y/n)"
if ($confirmation -eq 'y') {
	foreach ($package in $packages) {
		Invoke-Expression "& .\nuget.exe push ""$package"" -Source https://nuget.pkg.github.com/Rene-Sackers/index.json -SkipDuplicate"
		Invoke-Expression "& .\nuget.exe push ""$package"" -Source https://api.nuget.org/v3/index.json -SkipDuplicate"
	}
}
