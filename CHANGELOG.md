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

## [1.2.1] - 2026-11-04

### 修复

- **远程画面模糊仍未消除 → .rdp 参数整体回退到 v1.0.x 形态**：v1.2.0 回退视觉键后
  仍有残留嫌疑的参数（`smart sizing:i:1` 位图拉伸缩放、`dynamic resolution:i:1`、
  `winposstr` 窗口摆放、无损效率键组）全部移除。`.rdp` 只保留连接必需键
  （地址/账号/CredSSP/screen mode/desktopsize/剪贴板/磁盘共享/控制台），
  画质完全由 mstsc 默认值决定 —— 与 v1.0.x（字体清晰时期）行为一致。
- 保留项：剪贴板 `redirectclipboard:i:1` 与全盘 `drivestoredirect:s:*` 内置恒开
  （用户明确要求的功能，与画质无关）；界面排版修复全部保留。

### 移除

- 分辨率「最大化（铺满可用区域，不遮任务栏）」档及其 `winposstr`/`smart sizing`/
  `dynamic resolution` 依赖，默认分辨率恢复 `1920*1080`；`RdpOptions` 精简为
  (Host, Port, User, Password, W, H, ForceConsole)。

### 变更

- 程序集版本 1.2.0 → 1.2.1

## [1.2.0] - 2026-11-04

### 修复

- **远程画面字体渲染模糊**：回退「流畅优先」档中所有影响画面视觉质量的
  `.rdp` 键。根因（mstsc 侧三层叠加）：`session bpp:16` 下 ClearType 子像素
  渲染不可用；`connection type:1`（低带宽档）禁用字体平滑并触发激进压缩；
  `allow desktop composition:0` 关闭 DWM 合成使 ClearType 退化为灰度平滑。
  回退后 32 位色 + ClearType 恢复，字体恢复清晰。
- 明确**保留**的无损优化：`compression:i:1`、`bitmapcachepersistenable:i:1`、
  `audiomode:i:2`、打印机/串口/智能卡/POS/WebAuthn 重定向全关 ——
  均只降低传输/通道开销，不改变解码后的像素值。

### 移除

- **「流畅优先」选项**：界面复选框、分组覆盖项、持久化键读写与
  `AppendPerformanceKeys` 整体删除，替换为无条件写出的无损效率键
  （见上）。旧库残留键读到即忽略。
- **「共享剪贴板」「共享本地磁盘」界面开关**：两项内置默认开启
  （`.rdp` 恒写 `redirectclipboard:i:1` 与 `drivestoredirect:s:*`），
  界面与分组覆盖不再提供；`RdpOptions` 相应精简。

### 变更

- 程序集版本 1.1.0 → 1.2.0
- 分组设置窗口仅保留「分辨率」覆盖；主界面「显示与挂载」仅剩
  分辨率与「强制控制台模式」

## [1.1.0] - 2026-11-03

### 新增

- **流畅优先档（默认开）**：`session bpp:16`、`connection type:1`、关闭远程合成/壁纸/
  窗口拖动/菜单动画、`bitmapcachepersistenable:1`、外设重定向全关
  （依据跳板机 RDP 卡顿优化方案 B 组，落到工具默认参数）
- **盘符固定全盘双向**：取消盘符选择，固定写入 `drivestoredirect:s:*`
  （含连接后新插入的 U 盘）
- **远程桌面窗口最大化档**：分辨率首项「最大化（铺满可用区域，不遮任务栏）」，
  `.rdp` 写 `winposstr:s:0,3,...`（ShowCmd=3，区别于会盖住任务栏的全屏）
- **连接即自动保存**：启动器成功发起后落库，异常不落库

### 修复

- **主窗口拖动时尺寸被逐渐压小**：`WM_GETMINMAXINFO` 四层锁尺寸，只允许平移
- **输入框文字裁半、密码圆点不可见**（v1.0.0 即有）：样式 `Padding="12,8"`
  上下边距压缩可视区，六个输入框统一 `Padding="12,0"`
- **表单连接覆盖他行**：原以选中行为命中目标会静默改写其他记录，
  改为按 主机+端口+账号 精确匹配

### 变更

- **排版按 v1.0.0 基准恢复**：服务端设置两端分布、用户名/密码各占一行、
  左栏一屏可见无滚动、工具磁贴自然高度吸底
- 程序集版本 1.0.5 → 1.1.0；关于窗口版本号改读程序集
- 回归基线 122 → 194 项

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
