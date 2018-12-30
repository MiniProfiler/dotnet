# Setup Redis with Nuget
nuget install redis-64 -excludeversion
redis-64\tools\redis-server.exe --service-install; redis-64\tools\redis-server.exe --service-start

# Setup PostgreSQL, MySQL, MongoDB using Chocolatey
choco feature enable -n=allowGlobalConfirmation
choco install mongodb --version 3.6.0
choco install mysql --version 5.7.18 
choco install postgresql --version 9.6.8 --params "/Password:$PGPASSWORD"

# Chocolatey profile
$ChocolateyProfile = "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"
if (Test-Path($ChocolateyProfile)) {
    Import-Module "$ChocolateyProfile"
}
refreshenv

mysql -e "create database test;" --user=root
createdb test 

mkdir D:\data\db
# Start MongoDB as run as Background Process
Start-Process "C:\Program Files\MongoDB\Server\3.6\bin\mongod.exe" -ArgumentList "--dbpath D:\data\db" -NoNewWindow -RedirectStandardOutput log.txt

# Build Variables at https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=vsts#build-variables
.\build.ps1 -CreatePackages $true -PullRequestNumber $env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER