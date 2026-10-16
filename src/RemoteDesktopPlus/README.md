# 远程桌面连接++

轻量的 Windows 远程桌面连接管理工具：集中维护连接记录、加密保存凭据、批量在线检测、本机远程桌面端口管理。

> 工程代号 `RemoteDesktopPlus`，界面品牌名「小马远程桌面」，产物为 `远程桌面连接++.exe`。

## 功能

- **连接记录管理** —— 增删改查、拖拽排序（`GongSolutions.WPF.DragDrop`）、双击直连
- **凭据加密** —— 记录级 DPAPI（`D1$` 前缀）/ 主密码 AES-256-GCM（`M1$` 前缀）双形态；导入导出时的全量重加密与失败回滚
- **启动密码** —— PBKDF2-SHA256（200,000 次迭代）派生 + 定时安全比较
- **批量在线检测** —— 并发 TCP 探活（1.5s 超时）
- **本机远程桌面服务管理** —— 开关 RDP、改端口、自动维护 Windows 防火墙入站规则（需管理员权限）
- **托盘常驻** —— 最小化即隐藏、关闭即隐藏、右键打开/退出
- **单实例** —— 命名互斥体 + 命名管道把已有窗口带到前台

## 技术栈

| 层 | 选型 |
|---|---|
| 运行时 | .NET 8（`net8.0-windows`） |
| UI | WPF（MVVM，`WindowChrome` 自绘标题栏 + 设计令牌样式系统） |
| MVVM | CommunityToolkit.Mvvm 8.3（源生成器） |
| 拖拽 | GongSolutions.WPF.DragDrop 3.0 |
| 存储 | Microsoft.Data.Sqlite（`SQLitePCLRaw.bundle_e_sqlite3` 2.1.6） |
| 托盘 | WinForms `NotifyIcon`（仅此一处使用 WinForms） |

## 工程结构

```
RemoteDesktopPlus\          App 组合根、单实例、命名管道唤醒
RemoteDesktopPlus.Models\   数据模型与跨层 DTO
RemoteDesktopPlus.Services\ 服务层（数据库、加密、RDP 后端、探活、备份、托盘）
RemoteDesktopPlus.ViewModels\ 视图模型（MainViewModel 按职责分 partial）
RemoteDesktopPlus.Views\    视图与 XAML（.cs 为 BAML 载入壳）
RemoteDesktopPlus.Converters\ 值转换器
Styles\                     设计令牌（Colors.xaml）与控件样式（Controls.xaml）
tests\RemoteDesktopPlus.Tests\ xUnit 单元测试
libs\                       随仓库分发的第三方依赖
```

分层是单向的：`Models ← Services ← ViewModels`，`Views → ViewModels`；ViewModel 不引用任何 View 类型（对话框经 `IDialogService` 注入）。

## 构建与运行

需要 .NET 8 SDK 与 Windows。

```powershell
# 构建
dotnet build RemoteDesktopPlus.csproj -c Release

# 运行测试
dotnet test tests\RemoteDesktopPlus.Tests\RemoteDesktopPlus.Tests.csproj -c Release

# 发布（RID 专用，与既有发布包布局一致）
dotnet publish RemoteDesktopPlus.csproj -c Release -r win-x64 --self-contained false -o publish
```

## 数据与凭据

- 数据库随程序同目录：`data.db`（SQLite），首次启动自动建表。
  - `Connections(Id, Host, Port, UserName, Password, Remark, SortOrder)`
  - `Settings(Key, Value)`
- 凭据两种密文前缀：
  - `D1$` —— 当前用户 DPAPI（`RemoteDesktopPlus.Salt.v1`），不可跨机
  - `M1$` —— AES-256-GCM + PBKDF2-SHA256(200,000)，可跨机迁移
- 主密码校验依赖 `master.key.salt` 与 `master.key.verifier`（AES-GCM 加密的 `OK` 哨兵值）。
- 连接时凭据经 `CredentialStore`（`CredWriteW`/`CredReadW`）写入 Windows 凭据管理器，**不经过命令行**。

> `data.db`、`*.rdp` 已在 `.gitignore` 中，不会被提交。程序首启会在自身目录自建空库。

## 权限

「本机远程桌面」页签涉及写 `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server` 与 `netsh advfirewall` 规则，需以管理员身份运行；权限不足时抛出明确异常而非静默失败。

## 许可

MIT License，见 [LICENSE](LICENSE)。
