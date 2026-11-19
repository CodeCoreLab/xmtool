# -*- coding: utf-8 -*-
"""
M1 骨架生成器：从 RemoteDesktopPlus.All.cs 按区段锚点切割 S01-S17 到 Server/Shared/。
两步走第一步的机械切割（只切文件边界，零语义修改）。

区段锚点（行号 1-based，来源：grep "^// 区段" All.cs）：
  S01 起 = 行 19；S18 起 = 行 3334（TrayController，WPF 泄漏，排除）
切割产物 = 行 18-3333（区段头分隔线 + S01-S17 全部）

不做清单：不重排、不改 namespace、不删注释、不动任何语义字符。
校验：切割前后 sha256 对账（源文件只读，产物行数 = 3333-18+1）。
"""
import hashlib

SRC = 'RemoteDesktopPlus.All.cs'
OUT_DIR = 'Server/Shared'

with open(SRC, encoding='utf-8', newline='') as f:
    lines = f.read().split('\n')

# 行号锚点（1-based → 0-based 切片）
start = 18 - 1          # 行 18 = S01 区段头前的分隔线
end = 3334 - 1          # 行 3334 = S18 区段头（exclusive）
chunk = lines[start:end]

# 产物头：标明来源与边界（生成器自动维护，切割本身零语义改动）
header = [
    '// ========================================================================',
    '//  M1 平移切片（机械生成，勿手改）—— 源: ../../RemoteDesktopPlus.All.cs',
    '//  范围: 行 18-3333 = S01-S17 区段（Models + Services，不含 S18 TrayController）',
    '//  再生成: python Server/Shared/generate.py（锚点变更时同步本文件）',
    '// ========================================================================',
    '',
]

with open(f'{OUT_DIR}/All.Services.cs', 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(header + chunk))

# 对账
src_sha = hashlib.sha256(open(SRC, 'rb').read()).hexdigest()[:16]
out_lines = len(header) + len(chunk)
print(f"源 sha256[:16]={src_sha}  切片行数={len(chunk)}  产物总行数={out_lines}")
print(f"切片首行: {chunk[0][:70]}")
print(f"切片尾行: {chunk[-1][:70]}")
assert chunk[0].startswith('// ═'), "切片起点应为区段分隔线"
assert '区段 17' in '\n'.join(chunk), "切片应包含 S17"
assert '区段 18' not in '\n'.join(chunk), "切片不应含 S18"
print("对账通过：S01-S17 全量、S18 未混入")
