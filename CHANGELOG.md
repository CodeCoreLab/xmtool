# 变更日志 — 小马远程桌面（xmdesktop）

本文件记录本项目的变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [未发布]

### 修复

- **消除 `xUnit2013` 警告**：集合大小断言由 `Assert.Equal(1, Count)` 改为
  `Assert.Single`

### 变更

- **依赖改用 NuGet `PackageReference`**：移除 2 个 `<Analyzer>` HintPath 与
  6 个 `<Reference HintPath>`，改为 4 条 `PackageReference`
  （CommunityToolkit.Mvvm 8.3.2 / gong-wpf-dragdrop 3.2.1 /
  Microsoft.Data.Sqlite 8.0.10 / SQLitePCLRaw.bundle_e_sqlite3 2.1.6），
  版本与原 vendored DLL 一致，行为不变
- **移除仓库内 vendored 二进制**：删除 `libs/` 与 `analyzers/` 下 10 个 DLL，
  并从 git 历史中一并清除。理由：vendored DLL 无法被 `dotnet restore` 更新、
  无法审计来源，会让克隆后的仓库体积膨胀、CI 缓存失效
- 测试工程移除 6 处 HintPath（`PackageReference` 依赖可传递）

## [1.0.5] - 2026-10-23

### 修复

- 修复列表区拥挤与表头被裁切

### 变更

- 文档：README 补上界面布局的渲染验证方法（绕开 `requireAdministrator`）

## [1.0.4] - 2026-10-22

### 新增

- 新增分组设置面板

## [1.0.3] - 2026-10-22

### 新增

- 新增最近连接排序
- 新增分组级配置继承
- 新增导出单条 `.rdp` 文件

## [1.0.2]

### 新增

- 本地磁盘共享支持「全部盘符」，按 RDP 规范修正驱动器语法

## [1.0.1]

### 新增

- 支持剪贴板重定向
- 支持本机 RDP 状态实时轮询

## [1.0.0] - 2026-10-20

### 新增

- 产品统一更名为「小马远程桌面」，程序集名与产物名同步
- 分辨率选项增加 1920×1200（16:10 宽屏）
- 单文件源码化 + 功能迭代（schema 迁移 / 日志 / 限流 / 备份校验 / DI 化）
