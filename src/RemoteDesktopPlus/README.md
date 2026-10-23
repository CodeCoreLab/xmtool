# 小马远程桌面

轻量的 Windows 远程桌面连接管理工具：集中维护连接记录、加密保存凭据、批量在线检测、本机远程桌面端口管理。

> 工程代号 `RemoteDesktopPlus`，产品名「小马远程桌面」，产物为 `小马远程桌面.exe`。

## 功能

- **连接记录管理** —— 增删改查、拖拽排序（`GongSolutions.WPF.DragDrop`）、双击直连
- **最近连接排序** —— 「最近连接」列显示相对时间（刚刚/N 分钟前/N 天前）；可切换按最近使用倒序，
  与手工拖拽排序互斥（自动排序时禁用拖拽，避免拖了看不到效果）
- **分组与分组级配置** —— 连接可归属分组，顶栏按组筛选；分辨率/盘符/剪贴板支持按组覆盖，
  未覆盖项一律继承全局（只存差异，不复制全局值）
- **导出单条连接为 .rdp** —— 右键导出，便于分享与排障对比；**文件不含密码**
- **剪贴板重定向** —— 本地与远程之间复制粘贴（`.rdp` 显式写 `redirectclipboard:i:1`，默认开启）
- **本地磁盘共享** —— 默认「全部盘符」（`drivestoredirect:s:*`），远程机 `\\tsclient\` 下可互拷任意盘文件，
  含连接后新插入的 U 盘；也可退化为只共享指定盘（默认开启）
- **凭据加密** —— 记录级 DPAPI（`D1$` 前缀）/ 主密码 AES-256-GCM（`M1$` 前缀）双形态；导入导出时的全量重加密与失败回滚
- **启动密码** —— PBKDF2-SHA256（200,000 次迭代）派生 + 定时安全比较
- **批量在线检测** —— 并发 TCP 探活（1.5s 超时）
- **本机远程桌面服务管理** —— 开关 RDP、改端口、自动维护 Windows 防火墙入站规则（需管理员权限）
  状态每 3 秒轮询刷新（监听状态会因服务停止/端口变更/进程挤占而变化，不能只在启动时取一次快照）
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

> **单文件源码形态**：全部应用层 C# 合并在 `RemoteDesktopPlus.All.cs`（39 个原始文件机械合并，每源文件对应一个块级 namespace 区段，语义与多文件形态一致）。历史多文件版本备份于 `../backup_pre_singlefile_20261019/`。

```
RemoteDesktopPlus.All.cs    全量应用源码（Models → Services → ViewModels → Views → Converters → App 组合根）
App.xaml                    应用资源装配（WPF 编译器要求独立存在）
Views\*.xaml                视图声明（x:Class 指向单文件内的 partial 窗口类）
Styles\                     设计令牌（Colors.xaml）与控件样式（Controls.xaml）
Properties\AssemblyInfo.cs  程序集特性（仅能在全局命名空间）
tests\RemoteDesktopPlus.Tests\ xUnit 单元测试（独立工程）
libs\                       随仓库分发的第三方依赖
```

分层是单向的：`Models ← Services ← ViewModels`，`Views → ViewModels`；ViewModel 不引用任何 View 类型（对话框经 `IDialogService` 注入）。单文件内按区段注释（`区段 NN / 39`）导航，扩展新功能时在对应层区段内追加类型即可。

## 构建与运行

需要 .NET 8 SDK 与 Windows。

```powershell
# 构建
dotnet build RemoteDesktopPlus.csproj -c Release

# 运行测试
dotnet test tests\RemoteDesktopPlus.Tests\RemoteDesktopPlus.Tests.csproj -c Release

# 发布（单文件 exe，框架依赖 win-x64）
dotnet publish RemoteDesktopPlus.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o publish_final
```

发布产物为**单个 `小马远程桌面.exe`（约 2.5 MB）**：托管 DLL 与原生 `e_sqlite3.dll` 全部内嵌，首次启动自动解压原生库到 `%TEMP%\.net\`；目标机器需已安装 .NET 8 Desktop Runtime。如需零前提分发，加 `--self-contained true -p:EnableCompressionInSingleFile=true`（产物约 70 MB）。

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
