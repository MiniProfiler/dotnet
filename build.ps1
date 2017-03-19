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

    Write-Host "Restoring $project..." -ForegroundColor "Magenta"
    dotnet restore ".\src\$project"

    Write-Host "Packing $project... (Suffix:" -NoNewline -ForegroundColor "Magenta"
    Write-Host $VersionSuffix -NoNewline -ForegroundColor "Cyan"
    Write-Host ")" -ForegroundColor "Magenta"
    dotnet pack ".\src\$project" -c Release -o $packageOutputFolder --version-suffix=$VersionSuffix /p:CI=true

    Write-Host "Done."
    wRite-Host ""
}