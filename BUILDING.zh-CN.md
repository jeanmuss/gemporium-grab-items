# 构建

需要 Windows PowerShell 或 PowerShell 7、Roslyn C# 编译器（Microsoft.Net.Compilers.Toolset 4.8.0 中的 tasks/net472/csc.exe）、本地游戏和 BepInEx 5.4.23.6。

```powershell
./build.ps1 -ManagedDirectory '游戏目录/Gemporium_Data/Managed' -BepInExCoreDirectory '游戏目录/BepInEx/core' -CompilerPath 'Roslyn目录/tasks/net472/csc.exe'
```

使用游戏自带框架引用，构建后在 dist 生成插件 ZIP 与 SHA256SUMS.txt。包内仅有插件和六份说明，不包含依赖、游戏文件或个人数据；不会自动安装。构建引用由使用者本地提供。
