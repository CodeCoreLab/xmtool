# 第三方组件与许可声明 — xmtool（小马远程桌面）

本文件列出本项目使用的第三方组件及其许可协议。

**本仓库不包含任何第三方二进制文件**——所有依赖一律通过语言原生的包管理机制
（`PackageReference` / `go.mod` / `Cargo.toml`）在构建时还原。
克隆本仓库后执行对应的构建命令，即可获得完整的依赖树。

## xmtool（小马远程桌面）

技术栈：C# / .NET 8 / WPF

| 组件 | 版本 | 许可 | 用途 |
|---|---|---|---|
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | 8.3.2 | MIT | MVVM 框架、源生成器（`[ObservableProperty]` / `[RelayCommand]`） |
| [gong-wpf-dragdrop](https://github.com/gongmin/WPFDragDrop) | 3.2.1 | MIT | DataGrid 拖拽排序 |
| [Microsoft.Data.Sqlite](https://github.com/dotnet/Sqlite) | 8.0.10 | MIT | SQLite 访问（`Microsoft.Data.Sqlite` 本体） |
| [SQLitePCLRaw.bundle_e_sqlite3](https://github.com/sqlite/sqlite-wasm) | 2.1.6 | Apache-2.0 | SQLite 原生库绑定 |

### 附带的原生二进制

`Microsoft.Data.Sqlite` 与 `SQLitePCLRaw` 会引入 **SQLite** 的原生编译产物
（`e_sqlite3.dll` / `libe_sqlite3.so` / `libe_sqlite3.dylib`）。

**SQLite 本身属于公有领域（public domain）**，作者 D. Richard Hipp 声明：

> The author disclaims copyright to this source code. In place of a legal notice,
> here is a blessing: May you do good and not evil. May you find forgiveness for
> yourself and forgive others. May you share freely, never taking more than you give.

各平台的原生库由 `SQLitePCLRaw` 的构建流程从 SQLite 官方发行版编译得到，
不包含 SQLite 之外的额外代码。`SQLitePCLRaw` 自身的许可见上表。

### 测试依赖

| 组件 | 许可 | 用途 |
|---|---|---|
| [xunit](https://github.com/xunit/xunit) | Apache-2.0 | 单元测试框架 |
| [xunit.runner.visualstudio](https://github.com/xunit/xunit) | Apache-2.0 | Visual Studio 测试适配器 |
| Microsoft.NET.Test.Sdk | MIT | 测试宿主 |

> 说明：`xunit` 及其适配器为 Apache-2.0，**未被链接进发布产物**——
> 它们仅存在于测试工程，不进入 `RemoteDesktopPlus` 可执行文件。

---
