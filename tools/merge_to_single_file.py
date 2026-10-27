"""Merge all app .cs files into one RemoteDesktopPlus.All.cs.

Mechanical transform per file:
  1. Collect leading `using ...;` lines (replayed inside the namespace block).
  2. Drop `namespace X;` file-scoped declaration.
  3. Wrap the remaining body in `namespace X { ... }` with original usings inside.
Partial class declarations across multiple blocks are legal C#.
Assembly attributes (Properties/AssemblyInfo.cs) stay a separate file.
"""
import re
from pathlib import Path

ROOT = Path(r"E:\xmtool\xmtool\src\RemoteDesktopPlus")
OUT = ROOT / "RemoteDesktopPlus.All.cs"

FILES = [  # dependency order: Models -> Services -> ViewModels -> Views -> Converters -> App
    "RemoteDesktopPlus.Models/OnlineStatus.cs",
    "RemoteDesktopPlus.Models/MasterPasswordPurpose.cs",
    "RemoteDesktopPlus.Models/ConnectionFormResult.cs",
    "RemoteDesktopPlus.Models/ConnectionItem.cs",
    "RemoteDesktopPlus.Services/SettingKeys.cs",
    "RemoteDesktopPlus.Services/Log.cs",
    "RemoteDesktopPlus.Services/DatabaseService.cs",
    "RemoteDesktopPlus.Services/CryptoService.cs",
    "RemoteDesktopPlus.Services/CredentialStore.cs",
    "RemoteDesktopPlus.Services/IRdpBackend.cs",
    "RemoteDesktopPlus.Services/WindowsRdpBackend.cs",
    "RemoteDesktopPlus.Services/IRdpLauncher.cs",
    "RemoteDesktopPlus.Services/RdpLauncher.cs",
    "RemoteDesktopPlus.Services/OnlineProbe.cs",
    "RemoteDesktopPlus.Services/IDialogService.cs",
    "RemoteDesktopPlus.Services/BackupService.cs",
    "RemoteDesktopPlus.Services/RdpServerService.cs",
    "RemoteDesktopPlus.Services/TrayController.cs",
    "RemoteDesktopPlus.ViewModels/MainViewModel.cs",
    "RemoteDesktopPlus.ViewModels/MainViewModel.Connections.cs",
    "RemoteDesktopPlus.ViewModels/MainViewModel.Connect.cs",
    "RemoteDesktopPlus.ViewModels/MainViewModel.ServerSettings.cs",
    "RemoteDesktopPlus.ViewModels/MainViewModel.SecuritySettings.cs",
    "RemoteDesktopPlus.Views/AboutWindow.cs",
    "RemoteDesktopPlus.Views/LoginWindow.cs",
    "RemoteDesktopPlus.Views/SetLaunchPasswordWindow.cs",
    "RemoteDesktopPlus.Views/MasterPasswordWindow.cs",
    "RemoteDesktopPlus.Views/EditConnectionWindow.cs",
    "RemoteDesktopPlus.Views/MainWindow.cs",
    "RemoteDesktopPlus.Views/WpfDialogService.cs",
    "RemoteDesktopPlus.Converters/BoolToBrushConverter.cs",
    "RemoteDesktopPlus.Converters/BoolToEyeIconConverter.cs",
    "RemoteDesktopPlus.Converters/BoolToOnOffConverter.cs",
    "RemoteDesktopPlus.Converters/BoolToToggleTextConverter.cs",
    "RemoteDesktopPlus.Converters/BoolToVisibilityConverter.cs",
    "RemoteDesktopPlus.Converters/InverseBoolToVisibilityConverter.cs",
    "RemoteDesktopPlus.Converters/StatusToBrushConverter.cs",
    "RemoteDesktopPlus.Converters/StatusToTextConverter.cs",
    "RemoteDesktopPlus/App.cs",
]

header = """// ============================================================================
//  小马远程桌面 —— 全量应用源码（单文件版）
// ----------------------------------------------------------------------------
//  本文件由 39 个原始 .cs 机械合并而成（备份: ../backup_pre_singlefile_20261019/），
//  合并规则：每个源文件对应一个「块级 namespace」区段，块内重放原文件 usings，
//  语义与多文件形态完全一致；partial 类跨块声明合法。
//
//  ⚠ 不可并入本文件的必要外部文件（WPF 编译器强制）：
//    * App.xaml + Views/*.xaml —— 视图声明（x:Class/clr-namespace 指向本文件内命名空间）
//    * Styles/Colors.xaml、Controls.xaml —— 设计令牌
//    * Properties/AssemblyInfo.cs —— 程序集特性（只能在全局命名空间）
//    * libs/*.dll —— 第三方依赖（既有，未新增）
//    * tests/ —— 独立测试工程
//
//  区段顺序（依赖方向）：Models → Services → ViewModels → Views → Converters → App 组合根
// ============================================================================

"""

blocks = []
for i, rel in enumerate(FILES, 1):
    src = (ROOT / rel).read_text(encoding="utf-8")
    lines = src.splitlines()
    usings, body = [], []
    in_using = True
    for ln in lines:
        s = ln.strip()
        if in_using and (s.startswith("using ") or s == "" or s.startswith("//")):
            if s.startswith("using "):
                usings.append(s)
            elif s.startswith("//"):
                body.append(ln)  # file header comments keep position
            continue
        if s.startswith("namespace ") and s.endswith(";"):
            in_using = False
            continue
        in_using = False
        body.append(ln)
    # trim leading/trailing blank lines of body
    while body and body[0].strip() == "":
        body.pop(0)
    while body and body[-1].strip() == "":
        body.pop()
    ns = re.search(r"^namespace\s+([\w.]+);", src, re.M).group(1)
    u = "\n".join(usings) if usings else "// (no usings)"
    b = "\n".join(body)
    blocks.append(
        f"// ════════════════════════════════════════════════════════════════════════\n"
        f"// 区段 {i:02d} / {len(FILES)}：{rel}  →  namespace {ns}\n"
        f"// ════════════════════════════════════════════════════════════════════════\n"
        f"namespace {ns}\n{{\n{u}\n\n{b}\n}}\n"
    )

OUT.write_text(header + "\n".join(blocks), encoding="utf-8")
print(f"written {OUT} lines={len((header + chr(10).join(blocks)).splitlines())}")
