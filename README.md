# XIVLauncher 代理启动器

一个只向 XIVLauncher 及其子进程注入代理环境变量的小型 Windows 启动器，不修改用户或系统的全局环境变量。

## 使用

1. 运行 `XIVLauncherProxy.exe` 打开设置。
2. 检查或选择 `XIVLauncher.exe`。
3. 填写代理地址，例如 `http://127.0.0.1:37777`。
4. 可使用“测试连接”确认代理端口正在监听。
5. 点击“生成启动快捷方式”。
6. 程序会在 `XIVLauncherProxy.exe` 所在目录生成 `XIVLauncherProxy.lnk`，以后通过该快捷方式直接启动。

普通运行 EXE 会打开设置界面；带 `--launch` 参数运行会读取已保存配置并直接启动。快速启动遇到配置丢失、路径失效等问题时，会自动打开设置界面。

配置保存在：

```text
%LocalAppData%\XIVLauncherProxy\config.json
```

程序设置的变量包括 `HTTP_PROXY`、`HTTPS_PROXY`，并可选择设置 `ALL_PROXY` 和 `NO_PROXY`。这些变量仅由 XIVLauncher 及其后续子进程继承。

## 构建

需要 .NET 6 SDK：

```powershell
dotnet build .\src\XIVLauncherProxy\XIVLauncherProxy.csproj -c Release
```

发布为依赖 .NET 6 Desktop Runtime 的单文件程序：

```powershell
dotnet publish .\src\XIVLauncherProxy\XIVLauncherProxy.csproj `
  -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:DebugType=None `
  -o .\dist
```
