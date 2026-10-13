# XIVLauncher Proxy 交接

## 当前主线

维护 Windows 上按快捷方式启用的代理、OTP 登录和便携式自动更新。当前仓库版本为 `3.3.0`，版本号以 `Directory.Build.props` 为准。

## 接手入口

- [README](../README.md) 与 [英文 README](../README.en.md)：功能、安全边界和构建命令。
- [v3.3.0 发布说明](release-notes/v3.3.0.md)：当前版本变更。
- `src/`：Domain、Application、Infrastructure、Windows 平台和 Avalonia 桌面模块。
- `tests/` 与 `scripts/`：验证、发布和说明门禁入口。
- 本地工具链、测试安装位置和覆盖操作约定见本地 `AGENTS.md`。

## STATUS-001：建立交接入口并核对 SDK 约束

- 日期：2026-10-13。
- 仓库：`huaxianyan/XIVLauncherProxy`，分支 `main`，整理前基线 `4c62d42`。
- 当前能力：快捷方式选择代理与快捷登录，离线导入身份验证器二维码，TOTP 使用 Windows DPAPI 保存，正式单文件支持校验后原地更新。
- 安全边界：DPAPI 保护范围是当前 Windows 用户，已取得同一用户权限的恶意程序仍可能取出密钥。测试与诊断材料需脱敏，真实密钥、账号数据和二维码不进入交接文档。
- 本轮完成：补交接和 README 入口，同步私有约定，将旧项目内隔离 SDK 指引改为机器级工具链匹配要求。
- 工具链阻塞：`global.json` 的 `latestPatch` 范围与当前共享 SDK 不匹配。后续构建先补齐兼容的机器级 SDK，保留版本锁定，不为单个项目下载 SDK。
- 验证边界：本轮未运行构建、测试、二维码解析或登录，没有读真实密钥、覆盖测试安装程序、修改账号配置或发版。既有项目内 SDK 目录保持原状，未核对其可用性。
- CI 与发布：此次文档推送后的工作流和线上 Release 未查询，没有推发布标签或触发人工发布。
- 下一步：解决 SDK 匹配后，按实际改动选用例验证。覆盖已安装程序前确认进程状态，OTP 验证使用专门测试数据，并单独确认真实登录授权。
