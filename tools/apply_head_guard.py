# -*- coding: utf-8 -*-
"""给 Aireport 两页 <head> 加资源依赖警示注释。

目的：把「改这里会怎样坏」的提醒写在 AI/人一定会读到的位置（资源引用旁），
而非只放在外部文档里（外部文档 AI 不会主动读）。
"""
import os

GUARD = """<!--
  ══════════════════════════════════════════════════════════════════════
  ⚠️  改这里的资源引用前必读（2026-10-21 事故教训）
  ══════════════════════════════════════════════════════════════════════
  1. tokens.css 必须存在，且必须排在 style.css 之前。
     全部设计变量（--brand / --n-ink / --sev-p0 …）只在 tokens.css 定义；
     style.css 通过 var() 消费它们。漏引 tokens.css 不会报错，
     只会让所有颜色静默回退成默认值 —— 表现为「整页白底」。
     而 style.css 仍返回 HTTP 200，用状态码完全查不出问题。

  2. 两个页面共用同一套资源，改任一处必须同步另一处：
         本页(/index.html)  <->  /product/index.html
     两页若引不同版本的 style.css，会出现一页正常、一页失色。

  3. 改动后必须实际验证变量是否解析出颜色值，不能只看 HTTP 200：
     浏览器控制台执行：
       getComputedStyle(document.documentElement).getPropertyValue('--brand')
     返回有值（如 #3B82F6）= 生效；返回空字符串 = tokens.css 没加载上。

  4. 升版本号只改 ?v= 参数，但两页要一起改（见第 2 条）。
  ══════════════════════════════════════════════════════════════════════
-->"""

TARGETS = [
    '/opt/aireport/web/aireport/index.html',
    '/opt/aireport/web/aireport/product/index.html',
]

MARK = '改这里的资源引用前必读'

for p in TARGETS:
    if not os.path.isfile(p):
        print('  跳过（不存在）:', p)
        continue
    s = open(p, encoding='utf-8').read()
    if MARK in s:
        print('  已存在，跳过:', p)
        continue

    done = False
    for anchor in ('<link rel="stylesheet" href="/product/assets/tokens.css?v=1.0">',
                   '<link rel="stylesheet" href="assets/tokens.css?v=1.0">'):
        if anchor in s:
            s = s.replace(anchor, GUARD + '\n' + anchor, 1)
            done = True
            break

    if not done:
        print('  [!] 未找到 tokens.css 锚点:', p)
        continue

    open(p, 'w', encoding='utf-8').write(s)
    print('  [OK] 已加注释:', p)
