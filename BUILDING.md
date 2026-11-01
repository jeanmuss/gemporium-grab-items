# Building

Requires Windows PowerShell/PowerShell 7, Roslyn C# compiler (Microsoft.Net.Compilers.Toolset 4.8.0, tasks/net472/csc.exe), a local Gemporium installation and BepInEx 5.4.23.6.

Run:

```powershell
./build.ps1 -ManagedDirectory 'GAME/Gemporium_Data/Managed' -BepInExCoreDirectory 'GAME/BepInEx/core' -CompilerPath 'ROSLYN/tasks/net472/csc.exe'
```

The script uses explicit game framework references with /nostdlib+, checks compilation, packages only the plugin and six documentation files, and writes SHA256SUMS.txt in dist. Supply your own game and loader files; do not commit them. The script does not install the plugin.
