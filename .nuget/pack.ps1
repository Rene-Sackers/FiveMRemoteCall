$version = Get-Date -Format "yy.M.d.HHmmss"
$nugetExePath = ".\nuget.exe"
$packedPath = ".\packed\"

$projects =
    "../src/Implementation/FiveMRemoteCall.Client/FiveMRemoteCall.Client.csproj",
    "../src/Implementation/FiveMRemoteCall.Server/FiveMRemoteCall.Server.csproj",
    "../src/Implementation/FiveMRemoteCall.Shared/FiveMRemoteCall.Shared.csproj"

if (Test-Path $packedPath)
{
	Remove-Item -Path $packedPath -Recurse
}

foreach ($project in $projects) {
    $argumentList = "pack ""$project"" -Version $version -OutputDirectory .\packed -Build"
    
    Invoke-Expression "& $nugetExePath $argumentList"
}

$localNugetPath = $env:LocalNugetPath
if (-not ($null -eq $localNugetPath))
{
	Copy-Item -Path ".\packed\*" -Destination "$localNugetPath"
}