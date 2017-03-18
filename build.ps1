$projectsToBuild =
    'MiniProfiler.Shared',
    'MiniProfiler',
    #'MiniProfiler.EF6',
    'MiniProfiler.Mvc',
    'MiniProfiler.Providers.RavenDB',
    'MiniProfiler.Providers.SqlServer',
    'MiniProfiler.Providers.SqlServerCe'

Write-Host "Hello and welcome to our elaborate build!"
Write-Host "Just kidding, this is a sanity check at the moment, it'll get more detailed."

foreach ($project in $projectsToBuild) {
    Write-Host "Working on $project`:" -ForegroundColor "Magenta"
    Push-Location ".\src\$project"

    Write-Host "Restoring $project..." -ForegroundColor "Magenta"
    dotnet restore
    Write-Host "Building $project..." -ForegroundColor "Magenta"
    dotnet build
    Write-Host "Packing $project..." -ForegroundColor "Magenta"
    dotnet pack

    Write-Host "Done."
    wRite-Host ""
    Pop-Location
}