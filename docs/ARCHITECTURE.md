# 小马远程桌面 — 架构文档

> **定位**：讲清「代码怎么组织、数据怎么流动、想扩展该改哪里」。
> 维护操作（构建/发布/排障）见同目录 `MAINTENANCE.md`。
> **最后更新**：2026-10-20 · **对应源码**：`src\RemoteDesktopPlus\RemoteDesktopPlus.All.cs`

---

## 1. 技术栈

| 层 | 选型 | 版本 | 说明 |
|---|---|---|---|
| 运行时 | .NET | 8.0（`net8.0-windows`） | x64，C# 12，`LangVersion=11.0` |
| UI 框架 | WPF | — | 自绘标题栏（`WindowChrome`）+ 设计令牌样式系统 |
| MVVM | CommunityToolkit.Mvvm | 8.3 | 源生成器驱动 `[ObservableProperty]` / `[RelayCommand]` |
| 拖拽 | GongSolutions.WPF.DragDrop | 3.0 | 列表排序的拖放交互（`IDropTarget`） |
| 存储 | Microsoft.Data.Sqlite | — | + `SQLitePCLRaw.bundle_e_sqlite3` 2.1.6 原生引擎 |
| 托盘 | WinForms `NotifyIcon` | — | **全工程唯一一处 WinForms 使用**，仅为托盘图标 |
| 测试 | xUnit | 2.9.2 | 122 项用例 |
| 第三方分发 | `libs\*.dll` | — | 随仓库分发的本地 DLL 引用（非 NuGet 还原） |

> 依赖以 `HintPath` 本地引用为主，故 `tests\RemoteDesktopPlus.Tests.csproj` 需逐条重述引用
> （否则报 CS0012「ObservableObject 在未引用的程序集中定义」）。

---

## 2. 分层架构

**依赖方向严格单向，无环**：

```
        ┌──────────────────────────────────────┐
        │  Views（表示层 · 含 XAML）             │
        │  6 个 Window + WpfDialogService       │
        └───────────────┬──────────────────────┘
                        │ 调用（单向）
        ┌───────────────▼──────────────────────┐
        │  ViewModels（视图模型层）              │
        │  MainViewModel（按职责分 4 个区段）    │
        └───────────────┬──────────────────────┘
                        │ 依赖接口，不依赖实现
        ┌───────────────▼──────────────────────┐
        │  Services（服务层）                    │
        │  数据库/加密/凭据/RDP/探活/备份/托盘    │
        └───────────────┬──────────────────────┘
                        │
        ┌───────────────▼──────────────────────┐
        │  Models（领域层 · 无任何依赖）          │
        │  ConnectionItem / OnlineStatus / ...  │
        └──────────────────────────────────────┘
```

### 关键约束（不可违反）

| 约束 | 原因 |
|---|---|
| `Models` 不引用任何其他层 | 作为纯数据载体，保证可被任意层安全使用 |
| `Services` 不引用 `ViewModels` / `Views` | 服务层可独立测试 |
| `ViewModels` **不引用任何 Window 类型** | 这是「VM 可被无 UI 测试」的前提；对话框经 `IDialogService` 注入 |
| `Views` 可以不引用 `Services`（只经 VM 或组合根注入） | 保持视图薄 |
| 组合根**只有一处**：`App.OnStartup` | 依赖装配集中可见，禁散落 |

---

## 3. 模块划分（39 个区段明细）

全部应用层源码在单一文件 `RemoteDesktopPlus.All.cs` 内，按区段组织：每个区段对应一个块级
`namespace` 并带来源注释。**新增功能在对应层的区段内追加即可**。

### 3.1 Models 层（区段 01–04 · 4 个类型）

| 区段 | 类型 | 职责 |
|---|---|---|
| 01 | `OnlineStatus` | 枚举：`Unknown / Online / Offline / Checking`，UI 状态色与文案的数据源 |
| 02 | `MasterPasswordPurpose` | 枚举：`Set / Unlock`，主密码对话框用途 |
| 03 | `ConnectionFormResult` | 「添加/编辑」对话框数据载体（`INotifyPropertyChanged`），**不含 WPF 控件类型** |
| 04 | `ConnectionItem` | 核心实体：`Id/Host/Port/UserName/EncryptedPassword/Remark/SortOrder/Index/Status` + 扩展列 `GroupName/LastConnectedAt/Protocol` |

### 3.2 Services 层（区段 05–18 · 14 个类型）

| 区段 | 类型 | 职责 | 关键实现要点 |
|---|---|---|---|
| 05 | `SettingKeys` | 设置键名常量 | 集中定义，避免字符串散落 |
| 06 | `Log` | 轻量滚动文件日志 | `%LOCALAPPDATA%\RemoteDesktopPlus\logs\`，30 天清理；**写失败绝不影响主流程** |
| 07 | `DatabaseService` | SQLite 持久化 + schema 迁移 | 见 §4 数据模型 |
| 08 | `CryptoService` | 凭据加解密 + 主密码 + 启动密码 | 见 §5 安全模型 |
| 09 | `CredentialStore` | Windows 凭据管理器封装 | `CredWriteW/CredReadW`，**密码不进命令行** |
| 10/11 | `IRdpBackend` / `WindowsRdpBackend` | 系统层 RDP 调用抽象与实现 | 抽象存在使得 `mstsc` 相关行为可测 |
| 12/13 | `IRdpLauncher` / `RdpLauncher` / `WindowsRdpLauncher` | RDP 发起流程编排 | 写凭据 → 生成 .rdp → 拉起客户端 → 延迟清理 |
| 14 | `OnlineProbe` | TCP 探活 | 纯 TCP 连通性（不依赖 RDP 协议）；失败路径**一律静默返回 false** |
| 15 | `IDialogService` | 对话框抽象（7 个成员） | VM → Views 的唯一通道 |
| 16 | `BackupService` / `IBackupFormat` / `DbFileBackupFormat` | 备份导出与导入 | 见 §6 备份与迁移 |
| 17 | `RdpServerService` | 本机 RDP 服务管理 | 注册表 `fDenyTSConnections` / `PortNumber` + `netsh advfirewall` 规则 |
| 18 | `TrayController` | 托盘常驻 | WinForms `NotifyIcon` 封装 |

### 3.3 ViewModels 层（区段 19–23 · 1 个部分类分 5 区段）

`MainViewModel` 按职责拆为 5 个区段（同一 partial 类）：

| 区段 | 职责 |
|---|---|
| 19 `MainViewModel.cs` | 字段、可观察属性、构造入口、选项加载/持久化、`ParseResolution` |
| 20 `.Connections.cs` | 列表加载、增删改、拖拽排序（`IDropTarget`）、在线扫描（含并发限流） |
| 21 `.Connect.cs` | 直连表单、连接发起、保存语义、凭据修复 |
| 22 `.ServerSettings.cs` | 本机 RDP 服务状态刷新、开关、改端口 |
| 23 `.SecuritySettings.cs` | 启动密码、主密码、导入导出编排 |

**注入的协作者**（构造参数，禁在类内 new 或读静态单例）：

```csharp
public MainViewModel(DatabaseService db, CryptoService crypto,
                     IDialogService dialogs, IRdpLauncher? launcher = null)
```

### 3.4 Views 层（区段 24–30 · 7 个类型 + 6 个 XAML）

| 区段 | 类型 | 说明 |
|---|---|---|
| 24 | `AboutWindow` | 关于窗口 |
| 25 | `LoginWindow` | 启动密码解锁（**依赖经构造注入**，不再直连静态单例） |
| 26 | `SetLaunchPasswordWindow` | 设置/修改启动密码 |
| 27 | `MasterPasswordWindow` | 主密码设置/解锁 |
| 28 | `EditConnectionWindow` | 添加/编辑连接 |
| 29 | `MainWindow` | 主窗口（`WindowChrome` 自绘标题栏） |
| 30 | `WpfDialogService` | `IDialogService` 的 WPF 实现——**全程序唯一把 VM 需求翻译为具体窗口处** |

> **XAML 与代码的绑定关系**：6 个 XAML 是 WPF 编译器强制要求的独立文件，其 `x:Class` 与
> `clr-namespace` 指向单文件内的对应 `partial` 类。**改程序集名时必须同步改 XAML 的 `assembly=` 属性**。

### 3.5 Converters 层（区段 31–38 · 8 个类型）

值转换器：`BoolToBrush` / `BoolToEyeIcon` / `BoolToOnOff` / `BoolToToggleText` /
`BoolToVisibility` / `InverseBoolToVisibility` / `StatusToBrush` / `StatusToText`。

### 3.6 组合根（区段 39）

`App`：单实例互斥体 + 命名管道唤醒 + 启动装配 + 全局兜底。

```
OnStartup 流程：
  ① 单实例检测（命名互斥体）──非首实例──▶ 命名管道唤醒已有窗口 ──▶ 退出
  ② 清理上次残留的临时 .rdp 文件
  ③ db.Initialize()  ──失败──▶ 日志 + 弹窗提示 + 优雅退出（不裸崩）
  ④ 启动密码校验（若已设）──取消──▶ 退出
  ⑤ 主密码解锁（若已设且未解锁）
  ⑥ 检测导入失败哨兵文件 data.db.import-failed ──存在──▶ 弹窗告警
  ⑦ 装配 MainViewModel(db, crypto, dialogs, WindowsRdpLauncher.Instance)
```

---

## 4. 数据模型与 schema 迁移

### 4.1 表结构

数据库文件 `data.db`，位于程序同目录，首次启动自动建表。

```sql
CREATE TABLE Connections (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Host            TEXT NOT NULL,
    Port            INTEGER NOT NULL DEFAULT 3389,
    UserName        TEXT NOT NULL DEFAULT '',
    Password        TEXT NOT NULL DEFAULT '',   -- 密文（带前缀），非明文
    Remark          TEXT NOT NULL DEFAULT '',
    SortOrder       INTEGER NOT NULL DEFAULT 0,
    -- v2 扩展列（迁移管线追加）
    GroupName       TEXT NOT NULL DEFAULT '',
    LastConnectedAt INTEGER NOT NULL DEFAULT 0, -- Unix 秒，0=从未连接
    Protocol        TEXT NOT NULL DEFAULT 'rdp'
);
CREATE TABLE Settings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL DEFAULT '');
```

### 4.2 迁移管线（扩展点 E1）

**位置**：`DatabaseService.Migrations`（静态元组数组）+ `ApplyMigrations()`。

**扩展方式**：向数组**追加**一项，永不修改历史项：

```csharp
private static readonly (string Version, string Sql)[] Migrations = {
    ("2", "ALTER TABLE Connections ADD COLUMN GroupName ..."),   // v1 基线由 CREATE TABLE 建立
    // ("3", "ALTER TABLE ... ADD COLUMN ..."),                   // 新增迁移写这里
};
```

**执行语义**：
1. 读 `Settings['schema.version']`，缺失视为 v1。
2. 逐项：版本 > 当前 → 在**独立事务**内执行 SQL + 写入新版本号；失败即中断（不留半迁移状态）。
3. **幂等防护**：若版本标记缺失但目标列已存在（`pragma_table_info` 探测），只补版本号不重复 ALTER。

**新库**：`Initialize()` 先用 `CREATE TABLE IF NOT EXISTS` 建 v1 基线，再走迁移补到最新版本。

### 4.3 数据校验边界

| 边界 | 处理 |
|---|---|
| `Port` 输入非数字/越界 | VM 侧校验 1–65535，非法给提示且不写库 |
| `Host` 空白 | 拒绝保存，提示「请填写主机 IP 地址」 |
| 空 `EncryptedPassword` | 存空串（非 NULL），解密返回空串 |
| 拖拽排序 | 事务内批量写回 `SortOrder`，保证顺序不丢 |
| `LastConnectedAt` 回写 | 仅在 `Id > 0` 且连接成功发起后 |

---

## 5. 安全模型

### 5.1 凭据双形态加密（核心设计）

密文自带**形式自解释前缀**，单条记录即可判断来源：

| 前缀 | 形态 | 密钥来源 | 可迁移 |
|---|---|---|---|
| `D1$` | 当前用户 DPAPI | 系统用户绑定 | ❌ 换机不可解 |
| `M1$` | AES-256-GCM + PBKDF2-SHA256 | 主密码派生 | ✅ 可跨机 |

**M1$ 装机格式**：`"M1$" + Base64( nonce(12) ‖ tag(16) ‖ cipher )`

| 常量 | 值 |
|---|---|
| `Pbkdf2Iter` | 200,000 次 SHA-256 迭代 |
| `KeyBytes` | 32（AES-256） |
| `NonceBytes` | 12 |
| `TagBytes` | 16 |

**两种形态可共存于同一库**（逐条独立判定前缀）。

### 5.2 主密码全量重加密

设置/移除主密码时**事务内**重写全部记录：

```
SetMasterPassword(newPwd, items):
  ① 逐条解密 → 用新密钥加密 → 暂存
  ② 事务内：写 salt + verifier + 逐条 UpdatePasswordColumn
  ③ 成功 → 切换到新密钥
  ④ 任一失败 → 数据库回滚 + 内存对象回滚 + 抛异常（不留半新半旧）
```

> **性能设计（P2-5）**：重加密走**单列 UPDATE**（`UpdatePasswordColumn`），只改 `Password` 字段，
> 其余 6 列不参与写入，避免全表写放大。

### 5.3 启动密码

- PBKDF2-SHA256 200,000 次派生 + **定长时间比较**（`FixedTimeEquals`，防时序侧信道）
- 校验值存 `Settings`，salt 与 hash 分离存储
- 连续失败 5 次 → 程序自动退出（不提供无限试错窗口）

### 5.4 连接时密码传递（关键安全性质）

```
旧做法（已废弃）：cmdkey.exe /generic:TERMSRV/<host> /pass:<明文>
   → 密码出现在进程命令行，任意进程可读 Win32_Process.CommandLine

现做法：
  ① CredWriteW(CRED_TYPE_GENERIC, TargetName="TERMSRV/"+host, CredentialBlob=UTF-16LE(pwd))
     → 只经系统凭据存储，参数面天然不含密码
  ② 非托管内存用后逐字节清零再释放
  ③ 连接选项写入临时 .rdp 文件（UTF-16LE），客户端只接收文件路径
  ④ 5 秒后延迟清理临时文件与一次性凭据；启动时清扫上次残留
```

**测试守护**：`RdpLauncherTests` 用 `RecordingRdpBackend` 断言密码不出现于任何进程参数。

---

## 6. 备份与迁移（扩展点 E3）

`BackupService` 为门面，实际逻辑在 `IBackupFormat` 实现：

| 操作 | 实现 | 要点 |
|---|---|---|
| 导出 | `VACUUM INTO` | 让 SQLite 在引擎内生成**一致快照**，避免拷到「事务进行中」的半新半旧文件；不可用时回退文件拷贝 |
| 导入 | 文件头校验 → 覆盖 | 校验前 16 字节 = `"SQLite format 3\0"`；**拒绝非 SQLite 文件**，防止任意文件覆盖数据库 |
| 导入失败 | `.bak` 回滚 | 回滚前 `ClearAllPools` 释放连接锁 |
| 回滚也失败 | `data.db.import-failed` 哨兵 | App 启动时检测并弹窗告警（**绝不静默**） |

**扩展新格式**：实现 `IBackupFormat` 并在 VM 按扩展名分发（当前 `FormatId = "dbfile"`）。

---

## 7. 关键数据流

### 7.1 启动流程

```
App.OnStartup
  → 单实例检测 / 唤醒
  → RdpLauncher.CleanupTempFiles()
  → DatabaseService.Initialize()          [建表 + 迁移]
  → LoginWindow（启动密码，若有）
  → MasterPasswordWindow（解锁，若需要）
  → 哨兵文件检测
  → MainViewModel 构造
       ├─ LoadOptions()        [读 Settings 恢复选项，**抑制回写**防覆盖非法存储值]
       ├─ ReloadConnections()  [LoadAll → 排序 → 重编 Index]
       └─ RefreshServerStatus()[读注册表 + GetActiveTcpListeners]
  → MainWindow 显示 / TrayController 常驻
```

### 7.2 连接发起

```
双击列表项 / ConnectCommand
  → ParseResolution(分辨率字符串) → (width, height)
  → IRdpLauncher.Launch(RdpOptions)
       ├─ [成功发起] CredentialStore.StoreCredential()   ← 密码唯一出口（非命令行）
       ├─           BuildRdpContent() → WriteRdpFile()
       ├─           IRdpBackend.StartClient(rdpPath)
       └─           Task.Delay(5000) → 清理临时文件 + 凭据
  → 回写 LastConnectedAt（Id > 0）
  → [异常] 日志 + Error 弹窗，不崩 UI、不误写时间戳
```

### 7.3 批量在线扫描

```
ScanOnlineCommand（CanExecute = CanScan，扫描期间禁用防叠加）
  → IsScanning = true
  → SemaphoreSlim(20) 限流
  → 逐条：Status=Checking → OnlineProbe.IsOnlineAsync(host, port, 1500ms) → Online/Offline
  → IsScanning = false
```

> **为何限流**：不限流时 `Task.WhenAll` 全量并发会在记录多时形成瞬时 socket 风暴。

### 7.4 选项持久化（含「抑制回写」机制）

构造加载选项时**不触发**写入——否则会把「非法但已存的值」覆盖成默认值。
仅用户主动变更时才写回 `Settings`。

---

## 8. 扩展指南

| 想做什么 | 改哪里 | 备注 |
|---|---|---|
| 加数据库字段 | `DatabaseService.Migrations` 追加一项 | 勿改历史项；自动处理老库升级 |
| 加新对话框 | ① `IDialogService` 加方法 ② `WpfDialogService` 实现 ③ VM 调用 | VM 层**禁**出现 Window 类型 |
| 换 RDP 启动方式 | 实现 `IRdpLauncher`，注入给 VM | 已实例化，当前生产用 `WindowsRdpLauncher` |
| 加协议类型 | `ConnectionItem.Protocol` 已预留字段 | 配套实现 `IRdpLauncher` 分支 |
| 加备份格式 | 实现 `IBackupFormat` | 门面按扩展名分发 |
| 加托盘菜单项 | `TrayController` | 唯一 WinForms 处，注意线程同步 |
| 加分辨率预设 | `MainViewModel.ResolutionOptions` + 同步测试断言 | 当前 11 项 |
| 加值转换器 | 新建 `IValueConverter` + XAML 声明实例 | 参照现有 8 个写法 |

### 已知技术债（P3 · 备案择机）

| 项 | 说明 |
|---|---|
| `MainViewModel` 单类仍偏大 | 已按职责分 5 区段，但仍是最大单元；若继续膨胀可考虑拆为协作对象 |
| 日志级别硬编码 | 当前无配置入口，全量记录 Info 及以上 |
| `IBackupFormat` 仅一个实现 | 扩展点已就位但未验证多格式场景 |

---

## 9. 部署链路架构（产品页与下载分发）

```
本机（Windows 工作站）
  │
  ├─ 源码改 → 构建 → 双版本 publish
  │
  ├─[产品页] tools/enqueue_product_page.py
  │           └─ 写队列 ~/.workbuddy/scripts/.state/local_queue/local_*.json
  │                    │
  │                    ▼
  │           deploy_report_worker.py（Windows 计划任务，PT15M）
  │                    ├─ SFTP 上传 html → 测试机
  │                    ├─ 静态部署 → /opt/aireport/web/aireport/XMTool/index.html
  │                    └─ 读回 sha256 校验 + 公网探活（比对字节数）
  │
  └─[安装包] scp → 测试机 /opt/aireport/web/aireport/download/{slim,full}/

测试机 example-test-host
  └─ Docker workbuddy-nginx（nginx:1.27-alpine）
       ├─ root /opt/aireport/web/aireport
       └─ proxy_pass http://127.0.0.1:8000/aireport-api/  → uvicorn 后端

访问（内网）
  ├─ 产品页    https://example-site.com/XMTool/
  ├─ 报告详情  https://example-site.com/#/bare/<rid>
  ├─ 精简版    https://example-site.com/download/slim/小马远程桌面.exe
  └─ 整合版    https://example-site.com/download/full/小马远程桌面.exe
```

### 关键约束（踩坑沉淀）

1. **上传接口只入库、不做静态部署**——落盘必须经 `deploy_report_worker`。
2. worker 队列路径 `~\.workbuddy\scripts\.state\local_queue\`（依 `SCRIPT_DIR` 推导）。
3. 队列 `title` 必须 `<code>-<系统名> <YYYY-MM-DD>`——`parse_title` 取短横线前段作目录名，
   无前缀则部署被**静默跳过**（只入库不落盘）。
4. `type` 仅接受 `daily/weekly/monthly/patrol/other`。
5. **下载目录必须在 `download/` 下**——`cleanup_reports.sh` 会清理 90 天前的 `<YYYY-MM-DD>` 目录。
6. 验证必须比对 body **字节数**——nginx `try_files` 对深层路径 SPA fallback 回吐首页（假 200）。
7. 静态落盘归 `root`（App 侧自动化以 root 部署），nginx 容器以 `root` 读，权限一致。

---

## 10. 测试架构

`tests\RemoteDesktopPlus.Tests\`（xUnit，独立工程，**不并入单文件源码**）

| 测试文件 | 覆盖范围 |
|---|---|
| `DatabaseServiceTests.cs` | CRUD、排序、`Settings` upsert、事务回滚、嵌套事务语义 |
| `CryptoServiceTests.cs` | 加解密往返、前缀语义、主密码重加密、启动密码定长比较 |
| `RdpLauncherTests.cs` | RDP 契约 + **密码不进命令行**安全性质断言 |
| `OnlineProbeTests.cs` | 探活语义 + 所有失败路径静默返回 |
| `ParseResolutionTests.cs` | 分辨率字符串解析边界 |
| `MainViewModelTests.cs` | VM 全流程（增删改/拖拽/导入导出）——**不引用任何 WPF 窗口类型** |
| `Iteration2Tests.cs` | schema 迁移、扩展列持久化、单列 UPDATE、备份校验、扫描限流 |

**测试替身**：`FakeDialogService`（`IDialogService` 替身）、`TempDb`（一次性临时库）、
`StubDropInfo`（`IDropInfo` 最小实现）、`RecordingRdpBackend`（记录调用过程）。

> **设计意义**：测试工程不引用任何 WPF 窗口类型却能驱动 VM 走完全部交互流程——
> 这是「VM → Views 已切断」最直接的证明。

---

## 11. 架构决策记录（ADR 索引）

| # | 决策 | 备选与理由 |
|---|---|---|
| 1 | **对话框经 `IDialogService` 注入** | 备选：VM 直接 new Window。否——会让 VM 无法脱离 UI 测试 |
| 2 | **凭据双形态（DPAPI + 主密码 AES-GCM）** | 备选：仅 DPAPI（不可迁移）/ 仅主密码（每次需解锁）。否——场景差异大，双形态可共存且按需切换 |
| 3 | **备份用 `VACUUM INTO`** | 备选：直接文件拷贝。否——拷贝撞上事务会得到半新半旧文件 |
| 4 | **源码单文件化（39 区段）** | 备选：保持多文件。取单文件——便于分发与整体阅读；用区段注释弥补导航性 |
| 5 | **扫描并发限流 20** | 备选：不限流。否——记录多时形成 socket 风暴 |
| 6 | **下载用固定槽位而非日期目录** | 备选：放日期目录。否——会被 90 天清理脚本删除，且链接每次要改 |
| 7 | **重加密走单列 UPDATE** | 备选：整行 UPDATE。否——全字段写放大，其余 6 列无变化 |

---

## 12. 参考

- 维护操作：`MAINTENANCE.md`
- 历史交接材料：`项目交接文档.md`
- 问题分级与落地记录：`架构与扩展性分析报告.md`
- 重构交付记录（含旧名哈希）：`重构收工报告.md`
