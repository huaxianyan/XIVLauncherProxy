# XIVLauncher Proxy

简体中文 | [English](README.en.md)

为 XIVLauncher 提供按快捷方式启用的本地代理和自动 OTP 登录。配置程序只管理设置和生成快捷方式，日常启动统一从对应快捷方式进入。

## 功能

每个启动配置可以独立选择：

- 只使用代理：关闭本次自动登录，停留在账号选择界面
- 只使用快捷登录：临时为指定账号启动自动登录
- 同时使用代理和快捷登录：应用代理并完成指定账号登录

快捷登录会等待 XIVLauncher 请求 OTP 后生成并提交当前验证码，完成后恢复启动前的自动登录设置。可以为多个账号分别保存 TOTP 密钥并生成独立快捷方式。

## 使用

1. 运行 `XIVLauncherProxy.exe` 打开配置程序。
2. 在「基础设置」中确认 `XIVLauncher.exe` 路径；需要代理时填写代理地址。
3. 在「启动配置」中选择代理、快捷登录或同时启用两者。
4. 使用快捷登录时，选择 XIVLauncher 中已保存且启用了 OTP 的账号，输入 Base32 TOTP 密钥或 `otpauth://` 地址。
5. 保存配置并生成快捷方式。
6. 以后通过 `XIVLauncher - 配置名称.lnk` 启动。

快捷登录需要在 XIVLauncher 中为账号启用 One-Time Password，并启用 `Enable XL Authenticator app/OTP macro support`。不需要永久打开 XIVLauncher 的自动登录。

> [!NOTE]
> 本软件不提供 Square Enix OTP 的申请、绑定、解绑或迁移功能。请先通过 Square Enix 官方流程自行绑定身份验证器，确认官方验证器能够正常生成验证码后，再把同一份 TOTP 密钥保存到本软件。请妥善保留官方恢复码；删除本软件中的密钥不会解除 Square Enix 账号上的 OTP 绑定。

## TOTP 密钥如何保存

> [!IMPORTANT]
> 快捷登录要求程序能够在不再次询问主密码的情况下生成验证码。这是一项明确的安全取舍：密钥文件被单独复制后不能直接使用，但已经取得当前 Windows 用户权限的恶意程序仍可能取得密钥。请只在自己控制且可信的电脑上启用。

普通配置保存在：

```text
%LocalAppData%\XIVLauncherProxy\config.json
```

其中包含代理地址、账号名、XIVLauncher 账号关联和功能开关，不包含 TOTP 密钥、Square Enix 密码或生成的验证码。

TOTP 密钥单独保存在：

```text
%LocalAppData%\XIVLauncherProxy\totp-credentials.json
```

文件中的 Base64 内容是 Windows DPAPI 生成的密文，不是可以直接导入身份验证器的 Base32 密钥。快捷方式也不包含密钥或验证码。

每份密钥都使用 Windows DPAPI 的 `CurrentUser` 范围加密。正常情况下：

- 只复制凭据文件到其他电脑后无法解密
- 同一台电脑上的其他 Windows 用户无法直接解密
- 必须进入保存密钥时使用的 Windows 用户会话，程序才能生成验证码
- 不需要额外保存一份加密密码

密钥只在生成验证码时于进程内短暂解密。程序不会主动把 TOTP 密钥、`otpauth://` 地址或生成的验证码写入日志。

## 安全边界与风险

DPAPI 的安全边界是当前 Windows 用户，而不是独立的密码保险库。已经以当前用户身份运行的恶意程序可能调用 DPAPI、读取进程内存或替换程序文件，从而取得密钥或验证码。加密使用的应用标识不是第二层密码，不能阻止同一用户权限下的恶意程序。

这是零交互自动登录的固有限制：程序必须能够自动解密，同一用户权限下的恶意程序理论上也可能做到。因此，本方案不能替代需要单独解锁的密码管理器，也不能抵御已经失陷的 Windows 用户会话。

建议：

- 为 Windows 账号设置可靠的登录密码或 Windows Hello
- 为系统盘启用 BitLocker
- 不运行来源不明的程序，不随意授予管理员权限
- 将 Square Enix 恢复码单独离线保存
- 怀疑电脑失陷时，在 Square Enix 账号侧重新绑定验证器

在配置界面点击「清除密钥」会删除对应账号当前保存的加密记录。文件系统备份或历史快照仍可能保留旧密文；发生真实的设备失陷时，应重新绑定验证器，而不只是删除本地文件。

## 代理范围

启用代理的配置会为 XIVLauncher 及其子进程设置 `HTTP_PROXY`、`HTTPS_PROXY`，以及可选的 `ALL_PROXY` 和 `NO_PROXY`。不会修改用户或系统的全局代理。本地 OTP 请求始终直接连接 `127.0.0.1`。

## 架构与平台

项目使用 .NET 10、Avalonia UI、MVVM 和模块化单体：

```text
XIVLauncherProxy.Domain
XIVLauncherProxy.Application
XIVLauncherProxy.Infrastructure
XIVLauncherProxy.Platform.Windows
XIVLauncherProxy.Desktop
```

Domain、Application、通用 Infrastructure 和 Avalonia UI 不依赖具体桌面 UI 框架之外的 Windows 实现。DPAPI、Windows 快捷方式和 Windows 路径发现集中在 `Platform.Windows`。当前正式支持 Windows；未来移植其他平台时需要补充对应的平台实现。

界面文案使用 `.resx` 资源。英文是缺失翻译时的回退语言，当前同时提供简体中文资源。

## 构建

需要 .NET 10 SDK：

```powershell
dotnet build .\XIVLauncherProxy.sln -c Release
dotnet test .\XIVLauncherProxy.sln -c Release
```

发布 Windows 自包含单文件：

```powershell
dotnet publish .\src\XIVLauncherProxy.Desktop\XIVLauncherProxy.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -o .\dist
```
