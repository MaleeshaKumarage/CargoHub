# Build the solution. Stops CargoHub.Api (and dotnet hosting it) so that the build
# can copy DLLs into the API output folder (avoids MSB3027 "file is locked" errors).
$ErrorActionPreference = "Stop"

# Stop named processes
Get-Process -Name "CargoHub.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "CargoHub.Launcher" -ErrorAction SilentlyContinue | Stop-Process -Force

# When run via "dotnet run" the process is "dotnet.exe". Stop any dotnet process
# whose command line is running CargoHub.Api so we can build.
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
  Where-Object { $_.CommandLine -match "CargoHub\.Api" } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

Start-Sleep -Seconds 2
dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
