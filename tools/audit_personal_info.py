# -*- coding: utf-8 -*-
"""全量个人信息 / 溯源线索扫描器。

覆盖：全部源码、配置、文档、脚本、HTML、构建元数据。
分类模式：
  A. 代码托管平台地址（GitHub / Gitee / GitLab / 自建 Gitea / 裸 git@）
  B. 仓库链接与远端配置
  C. 姓名 / 账号（拼音、中文、工号形态）
  D. 邮箱 / 手机号 / 即时通讯
  E. 内网资产（IP、主机名、内部域名）
  F. 本机绝对路径 / 家庭网盘路径
  G. 凭据（密码字面量、Token 前缀、密钥路径）
  H. 真实身份推断线索（用户名组合、公司标识）

输出：逐文件逐行命中，附分类与上下文。
"""
import os
import re
import sys

ROOT = r'E:\xmtool\xmtool'
# 排除：构建产物、备份、git 内部、二进制库、截图
EXCLUDE_DIRS = {'bin', 'obj', '.git', 'backup', '_verify-screenshots', 'screenshots', 'data', 'logs'}
EXCLUDE_EXT = {'.dll', '.pdb', '.ico', '.png', '.jpg', '.jpeg', '.gif', '.exe', '.zip', '.bak', '.db',
               '.shm', '.wal', '.cache', '.genie'}

# 分类模式（正则, 说明）
PATTERNS = [
    # A. 代码托管平台
    ('A-托管平台', re.compile(r'github\.com|githubusercontent|raw\.github|gitee\.com|gitlab\.com|gitcode\.com|bitbucket\.org|codeberg\.org', re.I)),
    ('A-裸git协议', re.compile(r'\bgit@[\w.-]+:|\bssh://git@')),
    # B. 仓库/远端配置（通用模式，不硬编码具体仓库名与主机名）
    #    只匹配「仓库 URL」形态，避免把普通英文词误报为仓库名
    ('B-仓库URL', re.compile(r'(?:ssh|https?|git)://[\w.@:/-]+\.git\b|\bremote\.[a-z]+\.url\b|\.git\b\s*[（(]')),
    ('B-远端配置', re.compile(r'\[remote\s+"[^"]+"\]|\bgit\s+remote\b', re.I)),
    # C. 姓名/账号
    ('C-拼音账号', re.compile(r'\b[a-z]+(?:\.[a-z]+)?@(?:[a-z]+\.)+(?:local|internal)\b', re.I)),
    ('C-中文姓名', re.compile(r'(?:作者|署名|copyright|版权)[：:]\s*[\u4e00-\u9fa5]{2,4}')),
    # 注：RDP 通用默认账号（Administrator / root 等）属协议约定，非个人信息，
    #     此处不列为命中项，仅在人工复核时留意。
    # D. 联系方式
    ('D-邮箱', re.compile(r'[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}')),
    ('D-手机号', re.compile(r'(?<!\d)1[3-9]\d{9}(?!\d)')),
    ('D-IM/社交', re.compile(r'(?:微信|QQ|钉钉|飞书)号|wechat|telegram|discord', re.I)),
    # E. 内网资产（通用模式：私网网段 + 内部域名后缀）
    ('E-内网IP', re.compile(r'\b(?:10|172|192\.168)\.\d{1,3}\.\d{1,3}(?:\.\d{1,3})?\b')),
    ('E-内部域名', re.compile(r'[\w-]+\.(?:com\.cn|local|internal|lan)\b', re.I)),
    ('E-主机名', re.compile(r'\b[\w-]+-(?:jump|relay)\b|\bgitea\.[\w.-]+', re.I)),
    # F. 路径
    ('F-本机绝对路径', re.compile(r'[A-Za-z]:\\Users\\|/c/Users/|C:/Users/|AppData\\Local|AppData/Local')),
    ('F-盘符绝对路径', re.compile(r'\b[A-Za-z]:\\(?:[\w\\-]+)')),
    ('F-网盘', re.compile(r'BaiduNetdisk|百度网盘|OneDrive\\|坚果云')),
    # G. 凭据
    ('G-密码字面量', re.compile(r'(?:password|passwd|pwd|secret|token)\s*[=:]\s*["\']?[^\s"\']{6,}', re.I)),
    ('G-口令形态', re.compile(r'\b[A-Z][a-z]@[0-9]{6,}\b')),
    ('G-Token前缀', re.compile(r'\b(?:sk-|ghp_|gho_|glpat-|AKIA|xox[baprs]-)')),
    ('G-密钥路径', re.compile(r'\bid_[a-z]+\b|id_rsa|id_ed25519|\.ssh[/\\]|MASTER_KEY|ENC_\w+')),
    # H. 身份线索
    ('H-公司标识', re.compile(r'广发证券|GF\s*Securities|稳定性保障中心', re.I)),
    ('H-署名与版权', re.compile(r'作者|copyright|版权|©')),
]

# 需要重点核实的白名单（正常技术内容，不应报为问题）
WHITELIST_HINTS = [
    'Administrator',        # RDP 默认账号名，非个人信息
    'localhost', '127.0.0.1', '0.0.0.0',
]


def iter_files():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in EXCLUDE_DIRS]
        for fn in filenames:
            ext = os.path.splitext(fn)[1].lower()
            if ext in EXCLUDE_EXT:
                continue
            yield os.path.join(dirpath, fn)


def scan_text_file(path):
    hits = []
    try:
        with open(path, 'r', encoding='utf-8', errors='strict') as f:
            lines = f.read().split('\n')
    except (UnicodeDecodeError, PermissionError, OSError):
        return None  # 非 UTF-8 文本，跳过（二进制另处理）
    for i, line in enumerate(lines, 1):
        for cat, pat in PATTERNS:
            for m in pat.finditer(line):
                hits.append({
                    'line': i,
                    'cat': cat,
                    'match': m.group(0),
                    'text': line.strip()[:160],
                })
    return hits


def main():
    results = {}
    skipped = []
    for path in sorted(iter_files()):
        rel = os.path.relpath(path, ROOT)
        h = scan_text_file(path)
        if h is None:
            skipped.append(rel)
            continue
        if h:
            results[rel] = h

    total = sum(len(v) for v in results.values())
    print('=' * 78)
    print('扫描根目录:', ROOT)
    print('命中文件数: %d   命中总数: %d' % (len(results), total))
    print('跳过（非 UTF-8，需单独处理）: %d' % len(skipped))
    for s in skipped:
        print('   -', s)
    print('=' * 78)

    # 按分类汇总
    from collections import Counter
    cat_count = Counter()
    for rel, hs in results.items():
        for h in hs:
            cat_count[h['cat']] += 1
    print('\n### 分类汇总')
    for cat in sorted(cat_count):
        print('  %-16s %d' % (cat, cat_count[cat]))

    # 逐文件明细
    print('\n### 逐文件明细')
    for rel in sorted(results):
        hs = results[rel]
        print('\n── %s  (命中 %d)' % (rel, len(hs)))
        for h in hs:
            print('   %-14s L%-4d %-34s | %s' % (h['cat'], h['line'], h['match'][:34], h['text'][:90]))
    return 0


if __name__ == '__main__':
    sys.exit(main())
