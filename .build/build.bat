@echo off
"../Source/.nuget/nuget.exe" install FAKE -Version 4.62.5
"../Source/.nuget/nuget.exe" install xunit.runner.console -Version 2.1.0

"FAKE.4.62.5/tools/FAKE.exe" build.fsx %*