@ECHO OFF
dotnet build benchmarks\MiniProfiler.Benchmarks\ -c Release
cls
.\benchmarks\MiniProfiler.Benchmarks\bin\Release\net46\win7-x64\MiniProfiler.Benchmarks.exe %*