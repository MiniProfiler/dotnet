param(
    [parameter(Position=0,Mandatory=$true)]
    [AllowEmptyString()]
    [string] $VersionSuffix
)
$packageOutputFolder = "$PSScriptRoot\.nupkgs"
$projectsToBuild =
    'MiniProfiler.Shared',
    'MiniProfiler',
    #'MiniProfiler.EF6',
    'MiniProfiler.Mvc5',
    'MiniProfiler.AspNetCore',
    'MiniProfiler.AspNetCore.Mvc',
    #'MiniProfiler.Providers.RavenDB',
    'MiniProfiler.Providers.SqlServer',
    'MiniProfiler.Providers.SqlServerCe'

Write-Host "Hello and welcome to our elaborate build!"
Write-Host "Just kidding, this is a sanity check at the moment, it'll get more detailed."

mkdir -Force $packageOutputFolder | Out-Null

Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
Get-ChildItem $packageOutputFolder | Remove-Item
Write-Host "done." -ForegroundColor "Green"

Write-Host "Building Version $Version of all packages" -ForegroundColor "Green"

foreach ($project in $projectsToBuild) {
    Write-Host "Working on $project`:" -ForegroundColor "Magenta"
	
	Push-Location ".\src\$project"

    Write-Host "  Restoring and packing $project... (Suffix:" -NoNewline -ForegroundColor "Magenta"
    Write-Host $VersionSuffix -NoNewline -ForegroundColor "Cyan"
    Write-Host ")" -ForegroundColor "Magenta"

	dotnet msbuild "/t:Restore;Pack" "/p:Configuration=Release" "/p:VersionSuffix=$VersionSuffix" "/p:PackageOutputPath=$packageOutputFolder" "/p:CI=true"

	Pop-Location

    Write-Host "Done."
    Write-Host ""
}