@ECHO OFF
dotnet build benchmarks\MiniProfiler.Benchmarks\ -c Release
if errorlevel 1 goto :end
:benchmarks
.\benchmarks\MiniProfiler.Benchmarks\bin\Release\net46\win7-x64\MiniProfiler.Benchmarks.exe %*
:end