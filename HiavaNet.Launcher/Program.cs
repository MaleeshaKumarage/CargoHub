using System.Diagnostics;

// Solution root: from Launcher output (e.g. bin/Debug/net8.0 or bin/x64/Debug/net8.0) go up to repo root
var launcherDir = AppContext.BaseDirectory;
var solutionRoot = Path.GetFullPath(Path.Combine(launcherDir, "..", "..", "..", ".."));
if (!Directory.Exists(Path.Combine(solutionRoot, "HiavaNet.Api")))
    solutionRoot = Path.GetFullPath(Path.Combine(launcherDir, "..", "..", ".."));
if (!Directory.Exists(Path.Combine(solutionRoot, "HiavaNet.Api")))
    solutionRoot = Path.GetFullPath(Path.Combine(launcherDir, "..", "..", "..", "..", "..")); // e.g. bin/x64/Debug/net8.0
var apiProject = Path.Combine(solutionRoot, "HiavaNet.Api", "HiavaNet.Api.csproj");
var portalDir = Path.Combine(solutionRoot, "portal");

if (!File.Exists(apiProject))
{
    Console.WriteLine("Error: HiavaNet.Api project not found at " + apiProject);
    return 1;
}

Process? apiProcess = null;
Process? portalProcess = null;

try
{
    Console.WriteLine("Starting API...");
    apiProcess = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project \"" + apiProject + "\" --urls \"http://localhost:5299\"",
            WorkingDirectory = solutionRoot,
            UseShellExecute = false,
            CreateNoWindow = false
        }
    };
    apiProcess.Start();

    Console.WriteLine("Waiting for API to be ready...");
    await Task.Delay(TimeSpan.FromSeconds(8));

    if (!Directory.Exists(portalDir))
    {
        Console.WriteLine("Portal folder not found at " + portalDir + ". Only API was started.");
        Console.WriteLine("Press Enter to stop the API and exit.");
        Console.ReadLine();
        return 0;
    }

    // On Windows, run the portal via cmd with UseShellExecute so the user's PATH (e.g. Node/npm) is available.
    // Visual Studio often doesn't inherit the same PATH as a normal shell, so this ensures npm can be found.
    if (OperatingSystem.IsWindows())
    {
        Console.WriteLine("Starting Portal (Next.js) at " + portalDir + " ...");
        var portalCmd = "/c \"cd /d \"" + portalDir + "\" && npm run dev\"";
        portalProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = portalCmd,
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = portalDir
            }
        };
        portalProcess.Start();
    }
    else
    {
        Console.WriteLine("Starting Portal (Next.js)...");
        portalProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = "run dev",
                WorkingDirectory = portalDir,
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };
        portalProcess.Start();
    }

    Console.WriteLine("");
    Console.WriteLine("API:    http://localhost:5299");
    Console.WriteLine("Portal: http://localhost:3000 (may take a moment to compile)");
    Console.WriteLine("");
    Console.WriteLine("If the Portal window did not open, ensure Node.js and npm are in your PATH.");
    Console.WriteLine("Press Enter to stop both and exit.");
    Console.ReadLine();
}
finally
{
    if (portalProcess is { HasExited: false })
    {
        Console.WriteLine("Stopping Portal...");
        portalProcess.Kill(entireProcessTree: true);
    }
    if (apiProcess is { HasExited: false })
    {
        Console.WriteLine("Stopping API...");
        apiProcess.Kill(entireProcessTree: true);
    }
}

return 0;
