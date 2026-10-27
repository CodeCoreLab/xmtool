# 小马远程桌面（XiaoMa Desktop）

[![License: MIT](https://img.shields.io/badge/License-MIT-8a2be2.svg)](LICENSE)[![Releases](https://img.shields.io/badge/Releases-latest-blue.svg)](https://github.com/CodeCoreLab/xmtool/releases)[![Platform: Windows](https://img.shields.io/badge/platform-Windows-0078D4.svg)]

轻量的 Windows 远程桌面连接管理工具。单机运行，不联网、不上云、无服务端；数据与密钥均不出本机。

> 工程代号 `RemoteDesktopPlus` · 当前版本 **v1.0.0**（2026-10-20）

---

## 快速导航

| 我要… | 去哪 |
|---|---|
| 接手维护（构建/发布/排障） | **`docs/MAINTENANCE.md`** ← 第一入口 |
| 理解代码结构（模块/数据流/扩展） | **`docs/ARCHITECTURE.md`** |
| 看源码 | `src/RemoteDesktopPlus/`（全量代码在 `RemoteDesktopPlus.All.cs`） |
| 拿安装包 | 仓库的 [Releases](https://github.com/CodeCoreLab/xmtool/releases) 页面 |
| 了解依赖与许可 | [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) |

---

## 项目空间结构

```
xmtool/
├── src/RemoteDesktopPlus/   源码（唯一代码真源）
├── docs/                    文档（MAINTENANCE / ARCHITECTURE / 历史材料）
├── releases/                发布产物归档（按版本分目录 · 只归档，不在此运行）
├── run/                     运行目录（当前使用版本的副本 + data.db，可双击）
├── tools/                   辅助脚本（合并/入队/页面验证）
├── logs/  data/             操作日志与状态快照
└── backup/                  历史备份（多文件版 / 更名前 / 原始二进制）
```

> `releases/`、`run/`、`data/`、`logs/`、`backup/` 均不纳入版本控制。

**`run\` 与 `releases\` 的分工**：程序把 `data.db` 建在 **exe 所在目录**
（`AppContext.BaseDirectory`，代码层不可分离）。若直接从 `releases\v1.0.x-日期\`
双击运行，会在制品目录里留下 `data.db`，且每换一个版本就多一个孤立库
（实测 v1.0.3/1.0.4/1.0.5 各留了一个）。

| 目录 | 用途 | 可双击运行 |
|---|---|---|
| `releases\` | 版本归档，只读性质 | **否** |
| `run\` | 当前使用版本 + `data.db` | **是** |

升级 = 把新 exe 从 `releases\` **复制**到 `run\`（不要移动，归档要留），
`run\data.db` 原地保留即继承全部记录与设置。

详细说明见 `docs/MAINTENANCE.md §1`。

---

## 功能

- **连接记录管理** —— 增删改查、拖拽排序、双击直连
- **剪贴板与磁盘共享** —— 剪贴板重定向（`redirectclipboard:i:1`）；磁盘默认「全部盘符」
  （`drivestoredirect:s:*`，含连接后新插的 U 盘），远程机 `\\tsclient\` 下可互拷任意盘
- **最近连接排序** —— 相对时间列（刚刚/N 分钟前/N 天前），可切换按最近使用倒序
- **分组与分组级配置** —— 按组筛选；分辨率/盘符/剪贴板可按组覆盖，未覆盖项继承全局；
  顶栏「组设置」面板配置
- **导出单条连接为 .rdp** —— 右键导出，便于分享排障；**不含密码**
- **本机 RDP 状态轮询** —— 监听状态每 3 秒刷新（原先只在启动时取一次快照，会显示过期状态）
- **凭据加密** —— 记录级 DPAPI（`D1$`）/ 主密码 AES-256-GCM（`M1$`）双形态，可共存、可切换、可跨机迁移
- **启动密码** —— PBKDF2-SHA256（200,000 次）+ 定长时间比较
- **批量在线检测** —— 并发 TCP 探活（1.5s 超时，20 路限流）
- **本机 RDP 服务管理** —— 开关远程桌面、改端口、自动维护防火墙入站规则
- **托盘常驻 + 单实例** —— 关闭即隐藏；重复启动唤醒已有窗口
- **备份迁移** —— `VACUUM INTO` 一致快照导出；导入前 SQLite 文件头校验

---

## 下载

前往仓库的 [Releases](https://github.com/CodeCoreLab/xmtool/releases) 页面下载对应版本。

| 版本 | 大小 | 前提 |
|---|---|---|
| 精简版（框架依赖） | 2.5 MB | 需 .NET 8 Desktop Runtime |
| 整合版（自包含） | 70 MB | 无（x64 Win10+） |

两个版本功能完全一致，只是分发方式不同：精简版需要系统已装 .NET 8 Desktop Runtime，
整合版自带运行时、拷贝即用但体积大。

> 历史版本归档在 Releases 页面的旧 tag 下。

---

## 技术栈

.NET 8 · WPF · CommunityToolkit.Mvvm · Microsoft.Data.Sqlite · xUnit · gong-wpf-dragdrop

依赖全部通过 NuGet `PackageReference` 声明（`RemoteDesktopPlus.csproj`），
仓库内不含任何第三方二进制。

---

## 开发

```bash
cd src/RemoteDesktopPlus

# 还原依赖并构建
dotnet build -c Release

# 跑单元测试（165 项）
dotnet test

# 产物
bin/Release/net8.0-windows/小马远程桌面.exe
```

需要 **.NET 8 SDK 或更高版本**。

### 依赖清单

| 包 | 版本 | 用途 |
|---|---|---|
| CommunityToolkit.Mvvm | 8.3.2 | MVVM 框架与源生成器 |
| gong-wpf-dragdrop | 3.2.1 | DataGrid 拖拽排序 |
| Microsoft.Data.Sqlite | 8.0.10 | SQLite 访问 |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.6 | SQLite 原生库绑定 |

完整许可说明见 [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)。

---

## 许可

MIT License，见 [`LICENSE`](LICENSE)。

