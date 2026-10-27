# 小马远程桌面 — 项目维护文档

> **定位**：接手本项目的第一入口。讲清「东西在哪、怎么构建、怎么发布、怎么排障」。
> 架构原理与模块划分见同目录 `ARCHITECTURE.md`。
> **最后更新**：2026-10-20 · **当前版本**：v1.0.0（构建号 20261020）

---

## 1. 项目空间总览

项目根目录：**`E:\xmtool\xmtool`**

```
E:\xmtool\xmtool\
├── src\                        源码（含完整 git 仓库，唯一的代码真源）
│   └── RemoteDesktopPlus\      .NET 8 WPF 工程
│       ├── RemoteDesktopPlus.All.cs   全量应用源码（3762 行 / 39 区段 / 43 类型）
│       ├── App.xaml                   应用资源装配
│       ├── Views\*.xaml               6 个视图声明（WPF 编译器强制独立）
│       ├── Styles\                    设计令牌与控件样式
│       ├── Properties\AssemblyInfo.cs 程序集元数据
│       ├── tests\                     xUnit 测试工程（122 项）
│       ├── libs\                      随仓库分发的第三方 DLL
│       ├── RemoteDesktopPlus.csproj   工程文件
│       └── .git\                      提交历史（唯一真源）
│
├── docs\                       文档
│   ├── MAINTENANCE.md          ← 本文档
│   ├── ARCHITECTURE.md         架构文档（模块划分、数据流、扩展点）
│   ├── 项目交接文档.md          历史交接材料（2026-10-16 基线）
│   ├── 架构与扩展性分析报告.md    P0-P3 分级问题清单与落地记录
│   ├── 重构收工报告.md          重构交付记录（含旧名哈希，历史留档）
│   ├── 个人信息与溯源线索清理清单.md
│   ├── README-工程说明.md       工程 README 副本
│   ├── 小马桌面工具-产品页.html   对内外展示产品页（部署到 aireportdev）
│   └── _verify-screenshots\     产品页三视口验证截图
│
├── releases\                   发布产物归档（按版本分目录）
│   └── v1.0.0-20261020\
│       ├── 小马远程桌面-v1.0.0-fdd-2.5MB.exe        框架依赖版
│       └── 小马远程桌面-v1.0.0-standalone-70MB.exe  自包含免依赖版
│
├── logs\                       构建/发布/部署操作日志
├── data\                       运行时数据与状态快照（非源码数据）
├── tools\                      辅助脚本（16 个，2026-10-20 清理后）
│   ├── merge_to_single_file.py       多文件→单文件合并（可复现）
│   ├── enqueue_product_page.py       产品页入队部署
│   ├── audit_personal_info.py        个人信息与溯源线索扫描
│   ├── check_html.py                 结构自检（标签配对 / --i 合法性）
│   ├── capture_app.py                截取程序真实界面（Win32 PrintWindow）
│   ├── verify_page.js                三档渲染校验（Playwright）
│   ├── verify_download.js            下载链接浏览器级验证（断言真实 exe）
│   ├── verify_modal.js               下载弹窗交互回归（27 项断言）
│   ├── verify_shot.js                界面截图 + 灯箱验证（16 项断言）
│   ├── shot_page.js                  产品页多视口截图
│   ├── shot_modal.js                 下载弹窗截图
│   ├── diag_layout.js                容器几何测量 + 布局审计
│   ├── diag_btn.js                   按钮精度诊断（宽度不足定位）
│   ├── diag_page.js                  页面布局诊断（溢出来源定位）
│   ├── diag_release.js               发布卡片几何+伪元素诊断（横条类瑕疵）
│   └── audit_align.js                桌面端数值化对齐审计
└── backup\                     历史备份
    ├── pages-20261020\               产品页三代快照（口语版/改写前/当前版）
    ├── backup_pre_rename_20261019\   更名前源码快照
    ├── backup_pre_singlefile_20261019\ 单文件化前快照
    └── exe_backup_v3_原版带托盘前.exe 原程序二进制（回滚用，**不随交付分发**）
    ├── backup_pre_singlefile_20261019\  单文件化前的多文件版本（40 个 .cs）
    ├── backup_pre_rename_20261020\      更名前的文件快照（16 文件）
    └── exe_backup_v3_原版带托盘前.exe     原始二进制副本（溯源留档）
```

### 数据流与真源关系

| 内容 | 真源位置 | 说明 |
|---|---|---|
| 代码 | `src\RemoteDesktopPlus\.git` | 一切改动经 commit，禁直接改 releases 里的产物 |
| 发布产物 | `releases\<版本>\` | 由源码构建生成，非手工编辑 |
| 线上分发包 | 测试机 `/opt/aireport/web/aireport/download/` | 见 §4 发布流程 |
| 运行时数据 | 用户机 exe 同目录 `data.db` | 随程序走，不进仓库 |
| 程序日志 | `%LOCALAPPDATA%\RemoteDesktopPlus\logs\` | 30 天滚动，用户机侧 |

---

## 2. 环境准备

| 依赖 | 版本要求 | 本机现状 | 说明 |
|---|---|---|---|
| .NET SDK | 8.0+ | `C:\Users\Administrator\.dotnet-sdk`（8.0.426） | **本机系统只有 Runtime，SDK 装在用户目录** |
| Windows | Win10+ x64 | — | WPF 仅 Windows |
| Node.js（仅页面验证用） | 22+ | `C:\Users\Administrator\.workbuddy\binaries\node\` | 跑 Playwright 校验脚本 |
| Python（仅工具脚本用） | 3.11+ | `...\binaries\python\envs\default\` | 依赖 paramiko（部署） |

**SDK 调用约定**（系统 PATH 无 dotnet，必须走全路径）：

```bash
export DOTNET_ROOT="C:/Users/Administrator/.dotnet-sdk"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
DOTNET="C:/Users/Administrator/.dotnet-sdk/dotnet.exe"
```

> **踩坑**：SDK 若丢失（版本目录被清理），从 `https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.426/dotnet-sdk-8.0.426-win-x64.zip`
> 下载解压即可。`builds.dotnet.microsoft.com` 在本机代理下 TLS 握手必败，勿用。

---

## 3. 日常维护流程

### 3.1 改代码

```bash
cd ./repo/src/RemoteDesktopPlus
# 1) 改前备份（铁律：暂停→备份→修改→验证）
# 2) 全部应用层代码在 RemoteDesktopPlus.All.cs 内按区段定位修改
#    （区段注释形如 "区段 19 / 39：MainViewModel.cs → namespace ..."）
# 3) 提交用精确 pathspec，禁 add -A
git add RemoteDesktopPlus.All.cs
git commit -m "fix: <一句话说明>"
```

**单文件源码约定**：新增功能在对应层的区段内追加类型，不要新建 .cs 文件（除非是 WPF 强制要求的 XAML）。
改完后如需重新生成单文件（例如从备份的多文件版本改的），跑 `tools\merge_to_single_file.py`。

### 3.2 构建与回归（每次改动必做）

```bash
cd ./repo/src/RemoteDesktopPlus
$DOTNET build RemoteDesktopPlus.csproj -c Release          # 期望：0 警告 0 错误
$DOTNET test tests/RemoteDesktopPlus.Tests/RemoteDesktopPlus.Tests.csproj -c Release
                                                          # 期望：122/122 通过
```

**回归基线**：122 项测试全通过。任何改动致测试数下降或失败，**禁止进入发布环节**。

### 3.3 冒烟测试（发布前必做）

```bash
# 启动产物，观察 12 秒是否存活 + 日志是否写入 + data.db 是否自动创建
"./repo/releases/v1.0.0-20261020/小马远程桌面-v1.0.0-fdd-2.5MB.exe"
# 预期：进程存活；%LOCALAPPDATA%\RemoteDesktopPlus\logs\ 出现当日日志；同目录生成 data.db
```

> **坑**：冒烟前先 `Stop-Process` 清残留进程。程序是单实例，旧实例存活时新实例会走「唤醒已有窗口」路径
> 立即退出（exit 0），看起来像启动失败，实为正常行为。

---

## 4. 发布流程

### 4.1 构建双版本产物

```bash
cd ./repo/src/RemoteDesktopPlus
# 精简版：框架依赖单文件（2.5MB），目标机需 .NET 8 Desktop Runtime
$DOTNET publish RemoteDesktopPlus.csproj -c Release -r win-x64 --self-contained false \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none \
  -o publish_final
# 整合版：自包含免依赖（70MB），拷走即用
$DOTNET publish RemoteDesktopPlus.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish_standalone
```

> **坑**：`publish_*` 目录不会自动清理旧文件。换名/换版本后务必手工删旧 exe，否则目录里新旧并存。
> 另：`EnableCompressionInSingleFile` 仅自包含模式支持（FDD 下报 NETSDK1076）。

### 4.2 归档 + 上线

```bash
# 1) 归档到 releases（版本目录 + 语义化文件名）
mkdir -p ./repo/releases/v1.0.1-<日期>
cp publish_final/小马远程桌面.exe      ./repo/releases/v1.0.1-<日期>/小马远程桌面-v1.0.1-fdd-2.5MB.exe
cp publish_standalone/小马远程桌面.exe ./repo/releases/v1.0.1-<日期>/小马远程桌面-v1.0.1-standalone-70MB.exe

# 2) 上传到测试机固定槽位（页面链接指向这里，永不变）
scp -i ~/.ssh/id_workbuddy -o ControlMaster=no publish_final/小马远程桌面.exe \
    root@example-test-host:/opt/aireport/web/aireport/download/slim/小马远程桌面.exe
scp -i ~/.ssh/id_workbuddy -o ControlMaster=no publish_standalone/小马远程桌面.exe \
    root@example-test-host:/opt/aireport/web/aireport/download/full/小马远程桌面.exe

# 3) 历史留存（可选，带版本号）
ssh -i ~/.ssh/id_workbuddy root@example-test-host \
  "cp /opt/aireport/web/aireport/download/slim/小马远程桌面.exe \
      '/opt/aireport/web/aireport/download/archive/小马远程桌面-v1.0.1-fdd.exe'"
```

**下载槽位约定**（重要，勿改）：

| 槽位 | URL | 用途 |
|---|---|---|
| 精简版 | `https://example-site.com/download/slim/小马远程桌面.exe` | 长期稳定分发 |
| 整合版 | `https://example-site.com/download/full/小马远程桌面.exe` | 含最新改动 |
| 界面截图 | `https://example-site.com/download/screenshots/app-main.png` | 页面 hero 展示（见 §4.7） |

### 4.2.1 产品页部署路径（免日期白名单，2026-10-20 起）

产品页 URL 为 **`https://example-site.com/XMTool/`**（不带日期层）。

**实现机制**：`deploy_report_worker.py` 中新增常量
`NO_DATE_CODES = {"XMTool"}`；命中该集合时写入 `WEB_ROOT/<code>/`，
否则维持原有 `WEB_ROOT/<code>/<date>/`。

| 系统 | 部署路径 | URL |
|---|---|---|
| **XMTool**（本页） | `WEB_ROOT/XMTool/` | `/XMTool/` |
| EASS / EBSS / GFINFO / ITIS / OPS… | `WEB_ROOT/<code>/<date>/` | `/<code>/<date>/` |

**设计理由**：产品页是常驻站点页面（非按日期归档的报告），URL 需恒定；
版本更新直接覆盖同名 `index.html`，外部收藏与分享链接永不失效。
按日期分目录是报告归档语义，不适用于常驻页。

**改动影响面（已回归验证）**：白名单机制对非白名单 code 零影响 ——
`EASS/GFINFO/ITIS/OPS/EBSS` 的路径与探活 URL 均保持含日期结构不变。
新增常驻页面时，把其 code 加入 `NO_DATE_CODES` 即可。

> ⚠️ 部署路径变更后需同步更新：`deploy_report_worker.py` 的 `static_deploy`
> 与 `static_deploy_with_retry`（**两处**，写入路径与探活 URL 必须一致，
> 否则探活会指向不存在的日期路径导致误判失败）。
| 历史 | `download/archive/` | 带版本号留存 |

> **为何不放日期目录**：aireport 的 `cleanup_reports.sh` 会删除 web 树中 90 天前的 `<YYYY-MM-DD>` 目录。
> `download/` 是固定名目录，不匹配该清理规则，故长期存活。**上传前务必确认路径在 `download/` 下**。

### 4.3 验证发布（必做，禁跳）

```bash
# 字节数必须与源文件精确一致（防 nginx SPA fallback 假 200）
ssh -i ~/.ssh/id_workbuddy root@example-test-host \
  "curl -s -o /dev/null -w '%{http_code} %{size_download}\n' \
   'https://example-site.com/download/slim/%E5%B0%8F%E9%A9%AC%E8%BF%9C%E7%A8%8B%E6%A1%8C%E9%9D%A2.exe'"
# 期望：200 + 字节数 == 本地文件大小
```

> **⚠️ 下载链接必须用绝对路径（`/download/...` 与 `/download/screenshots/...`），禁用相对路径**
>
> 产品页现部署于 **`/XMTool/`（免日期层根目录，2026-10-20 起）**——XMTool 已在
> `deploy_report_worker.py` 的 `NO_DATE_CODES` 白名单内，写入 `WEB_ROOT/XMTool/` 而非
> `WEB_ROOT/XMTool/<date>/`，URL 恒定、版本更新覆盖同名文件。
> 若误用相对路径（如 `screenshots/app-main.png`），会被解析为 `/XMTool/screenshots/...` ——
> 该路径不存在，nginx `try_files` 的 SPA fallback **返回 200 + HTML 首页**，
> 用户拿到的是网页而非图片/安装包（2026-10-20 实际事故）。
> **curl 直接测绝对路径会「通过」而掩盖此 bug（已验证的假绿）。**

### 4.4 页面视觉规范基线（改样式前必读）

产品页的样式系统已收敛为令牌驱动，**改样式请优先用令牌，禁止硬编码**：

| 维度 | 令牌 | 说明 |
|---|---|---|
| 字号 | `--fs-micro/xs/sm/base/lead/title/panel/h3/h2/display/metric` | 11 档。原 61 处模糊值（12/12.3/12.5/12.8）已收敛，禁再引入 |
| 间距 | `--s1..--s24`（8pt 网格） | 非 4 倍数间隙需有理由；2px 内微值允许 |
| 动效曲线 | `--ease-out-expo/quint`、`--ease-in-soft`、`--ease-io` | **禁 bounce/elastic**（回弹显廉价）；进入用 expo，状态切换用 quint |
| 动效时长 | `--t-instant/state/layout/enter/hero` | 对应 100/300/500 规则，禁超 500ms 的 UI 反馈 |
| 图标 | `--icon-sm/md/lg` + `--icon-stroke-lg/sm` | 统一 viewBox(24)+round；描边按尺寸分档（21px→1.9，≤17px→2） |
| 深色区文字 | `--stage-ink-strong/bright/dim`、`--stage-cyan-bright/soft` | 禁在深色区直接写 `#fff` / `#7DD3FC` |
| 代码高亮 | `--code-key/str/cmt/hl/ok` | 语法配色统一入口 |

**栏目动效约定**（8 类签名揭示，由 `--i` 驱动错峰）：

| 类名 | 效果 | 用于 |
|---|---|---|
| `reveal-cards` | 上浮 + 轻缩放 | 概览 / 场景 / 发布 |
| `reveal-stack` | 自上方落下 | 技术架构 |
| `reveal-rows` | 自左滑入 | 差异化对比 |
| `reveal-slide` | 分栏对进（配 `.from-right`） | 核心功能 |
| `reveal-digits` | 数字弹升 | 首屏统计 |
| `reveal-lines` | 密节奏逐行浮现 | 收尾段 / 页脚 |

> 新增栏目时按「内容语义」挑一种，不要重复既有栏目已用的类型。

### 4.5 改页面文件的铁律（2026-10-20 事故教训）

**必须用 `read → 处理 → 一次性 write` 的模式**，禁用「多次 `s.replace()` 后再拼接字符串」的写法 ——
曾因拼接逻辑出错把 110KB 页面写成 0 字节。

```python
s = open(P, encoding='utf-8').read()   # 读
s = s.replace(...)                     # 改（可多次，但都是纯字符串操作）
open(P, 'w', encoding='utf-8').write(s)  # 一次性写
```

**改前必备份**：`cp docs/小马桌面工具-产品页.html backup/<场景>_<日期>/`。
**恢复来源优先级**：本地 backup → 服务器 web 树副本（`scp` 拉回，为线上真实版本）。

### 4.6 浏览器级下载验证（发布下载功能后必做）

纯 URL 探测不够——必须真正点击页面按钮确认拿到 PE 文件：

```bash
cd ./repo/tools
NODE_PATH=C:/Users/Administrator/.workbuddy/binaries/node/workspace/node_modules \
  C:/Users/Administrator/.workbuddy/binaries/node/versions/22.22.2-6/node.exe verify_download.js
# 期望：两枚按钮均输出「头2字节 "MZ" ｜ PE=true ｜ 是HTML=false ｜ ✅ 有效 exe」
```

该脚本同时打印**链接解析后的绝对地址**，可直接暴露相对路径错误。

### 4.7 界面截图维护（页面展示用）

产品页 hero 右侧展示程序真实运行截图（`docs/screenshots/app-main.png`，1280×920）。

**重新生成截图**（改版后在演示库注入示例数据再截）：

```bash
# ① 填充演示数据（脚本内置，示例 IP 已脱敏，无真实凭据）
#    见 tools/capture_app.py 同目录的历史脚本，或直接用 sqlite3 写 Connections 表
# ② 运行程序并截取窗口
"<python-venv>/Scripts/python.exe" tools/capture_app.py
#    输出：docs/screenshots/app-main.png
# ③ 上传到固定目录（勿放日期目录，会被清理）
scp -i ~/.ssh/id_workbuddy docs/screenshots/app-main.png \
    root@example-test-host:/opt/aireport/web/aireport/download/screenshots/app-main.png
```

**⚠️ 三条约束**：

1. **截图必须脱敏**：示例 IP 用 `192.168.x.x` / `10.0.0.x` 等通用示例网段，
   禁用内部真实资产 IP；凭据字段一律留空。
2. **存放位置必须是 `/download/screenshots/`**（固定名目录）——
   放在 `<code>/<date>/` 日期目录下会被 90 天清理脚本一并删除。
3. **页面引用用绝对路径** `/download/screenshots/app-main.png`（相对路径在子路径部署下会解析错）。

**截图技术要点**（`capture_app.py`）：用 Win32 `PrintWindow` + `PW_RENDERFULLCONTENT(2)`
抓取 WPF 窗口合成层（普通 `PrintWindow` 抓不到 WPF 内容）；PNG 用手写 zlib 编码，
无第三方依赖。启动前先 `taskkill` 清残留实例（程序单实例，旧进程会抢占）。

**验证**：`tools/verify_shot.js` —— 检查截图加载/宽高比/alt、灯箱四种交互、
提示语已移出正文、桌面三档无溢出（16 项断言）。

### 4.8 下载弹窗交互验证（改动弹窗后必做）

产品页的下载入口是**弹窗选择器**（点「立即下载」/ 页脚入口 → 弹出两版本对比 → 卡内按钮下载）。
改动弹窗后跑完整交互回归：

```bash
cd ./repo/tools
NODE_PATH=C:/Users/Administrator/.workbuddy/binaries/node/workspace/node_modules \
  C:/Users/Administrator/.workbuddy/binaries/node/versions/22.22.2-6/node.exe verify_modal.js
# 期望：27 项全通过（打开/信息完整/选中切换/三种关闭/焦点还原/弹窗内真实下载/页脚入口）
```

**弹窗交互约定**（改动时勿破坏）：

| 行为 | 实现方式 |
|---|---|
| 打开 | `[data-dl-open]` 元素点击 → `open()`，并锁定背景滚动（`body.modal-open`） |
| 关闭 | `[data-dl-close]`（关闭按钮 / 遮罩）、`Esc` 键，共三种；关闭后焦点还原到触发按钮 |
| 焦点循环 | `Tab` / `Shift+Tab` 在弹窗内循环，不会跑出到背景 |
| 默认选中 | 打开时高亮 `dl-opt--pick`（精简版，带「推荐」徽标） |
| 卡片选中 | 点击卡片 / 聚焦 / Enter / Space 切换 `is-sel` 高亮；点卡内下载按钮不改变选中态 |
| 无障碍 | `role="dialog"` + `aria-modal="true"` + `aria-labelledby`；触发按钮带 `aria-haspopup="dialog"` |
| 动效 | `prefers-reduced-motion` 下取消位移，仅做显隐切换 |

> **注意**：主展示区（hero）与页脚只保留**打开弹窗**的入口；两枚实际下载链接位于弹窗内，
> 故 `verify_download.js` 会先点开弹窗再取链接（脚本已适配）。

---

## 5. 产品页维护

产品页 `docs\小马桌面工具-产品页.html` 是对外展示入口，部署在 aireportdev。

### 5.1 本地验证（改完页面必做）

```bash
cd ./repo/tools
NODE_PATH=C:/Users/Administrator/.workbuddy/binaries/node/workspace/node_modules \
  C:/Users/Administrator/.workbuddy/binaries/node/versions/22.22.2-6/node.exe verify_page.js
# 期望：三视口均为「水平溢出 0px / 8 画布全绘制 / reveal 全入场 / 无 JS 报错」
```

### 5.2 部署到内网

```bash
# 写入本机队列 → worker 自动完成上传 + 静态部署
"<python-venv>/Scripts/python.exe" ./repo/tools/enqueue_product_page.py
cd /c/Users/Administrator/.workbuddy/scripts
"<python-venv>/Scripts/python.exe" deploy_report_worker.py
```

**部署机制关键约束**（踩坑汇总，详见 `ARCHITECTURE.md §9`）：

1. aireport **上传接口只入库、不做静态部署**；落盘必须经本机 `deploy_report_worker`。
2. worker 队列路径是 `~\.workbuddy\scripts\.state\local_queue\`（依 `SCRIPT_DIR` 推导），
   不是 `~\.workbuddy\.state\`。
3. 队列 JSON 的 `title` **必须** `<code>-<系统名> <YYYY-MM-DD>` 格式——`parse_title` 取短横线前段作
   web 树目录名，无前缀则部署被**静默跳过**。
4. `type` 只接受 `daily/weekly/monthly/patrol/other`，其他值报「type 非法」。
5. 访问地址：`https://example-site.com/XMTool/`（code=XMTool）。
6. 验证必须比对 **body 字节数**：nginx `try_files` 对深层路径 SPA fallback 会回吐首页（假 200）。

---

## 6. 排障速查

| 现象 | 根因 | 处置 |
|---|---|---|
| 构建报 `dotnet: 命令未找到` | 系统 PATH 无 SDK | 用 §2 的全路径约定 |
| XAML 报找不到程序集 | `AssemblyName` 改了但 XAML 的 `assembly=` 未同步 | 两者必须同批改 |
| 冒烟「启动即退出」 | 单实例互斥体被旧进程持有 | 先清残留进程再测 |
| 发布目录新旧 exe 并存 | publish 不清理旧文件 | 手工删旧产物 |
| 下载到的是网页而非 exe（43769B HTML） | 页面用了**相对路径**，浏览器拼到 `/<code>/<date>/download/...` → SPA fallback 返回首页 | 改绝对路径 `/download/...`；用 §4.4 浏览器级验证 |
| 页面下载 200 但内容不对 | nginx SPA fallback 假 200 | 比对字节数 + 检查文件头 |
| 浏览器控制台 favicon 404 | 站点未配 favicon，浏览器自动请求 | 页面内联 `<link rel="icon" href="data:image/svg+xml,...">` |
| 部署后 404 | 标题缺 `<code>-` 前缀 / 路径不在 `download/` | 按 §4.2 §5.2 约束修正 |
| `data.db` 损坏无法启动 | 库文件损坏 | 程序会弹窗提示并退出；用同目录 `data.db.bak` 恢复 |
| 导入数据后提示「数据不一致」 | 上次导入回滚失败（哨兵文件存在） | 检查 `data.db.import-failed` 与 `data.db.bak` |

---

## 7. 版本与变更记录

| 版本 | 日期 | 主要内容 |
|---|---|---|
| v1.0.0 | 2026-10-20 | 统一更名为「小马远程桌面」；源码单文件化（39 区段）；双版本单 exe 分发；产品页上线 + 双下载入口 |

**历史沿革**（名称变更对照，便于检索旧资料）：
- 2026-10-16 前：产品名「远程桌面连接++」，产物 `远程桌面连接++.exe`，多文件源码结构
- 2026-10-19：源码单文件化（`RemoteDesktopPlus.All.cs`），产物改为单 exe
- 2026-10-20：统一更名为「小马远程桌面」，产物 `小马远程桌面.exe`

> 更名前的名称与哈希在 `docs\重构收工报告.md`、`docs\个人信息与溯源线索清理清单.md` 中按下**历史原样**保留，
> 检索旧资料时以该两处为准。

---

## 8. 铁律速查（维护时必守）

1. **发布前必须**：构建 0 警告 + 回归 122/122 + 冒烟通过，三者齐备。
2. **改文件前必须**备份（暂停→备份→修改→验证）。
3. **禁用 `git add -A`**，用精确 pathspec。
4. **下载路径必须在 `download/` 下**，勿放日期目录（会被 90 天清理）；页面链接必须**绝对路径**。
5. **验证看字节数 + 文件头**，不看状态码；下载功能必须走浏览器级点击验证。
6. **改名三处同批**：csproj `AssemblyName` + AssemblyInfo + 全部 XAML 的 `assembly=`。
7. `data.db` 与日志**不进仓库**（`.gitignore` 已覆盖）。
