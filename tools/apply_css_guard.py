# -*- coding: utf-8 -*-
"""强化 style.css 头部警示：把「依赖 tokens.css」写成不可忽略的警告块。"""
import io

P = '/opt/aireport/web/aireport/product/assets/style.css'

OLD = """/* ============================================================
   Aireport 产品介绍页 v2 · 单文件 · 零外部依赖
   白底 / 品牌蓝 / 高密度动效（粒子·数据流·视差·3D·生长时间轴）
   ============================================================ */"""

NEW = """/* ============================================================
   Aireport 产品介绍页 v2 · 单文件 · 零外部依赖
   白底 / 品牌蓝 / 高密度动效（粒子·数据流·视差·3D·生长时间轴）
   ------------------------------------------------------------
   ⚠️  本文件单独无效 —— 必须与 tokens.css 一同加载，且 tokens 在前

   本文件内所有 var(--brand…)/var(--n-…) 的「定义源」在 tokens.css。
   若使用方页面只引本文件、漏引 tokens.css：
     · 浏览器不会报任何错误（CSS 变量未定义 = 静默回退默认值）
     · 本文件仍返回 HTTP 200，用状态码查不出问题
     · 页面表现为「底色全白、无品牌色、卡片无阴影」——整页失色

   因此：改本文件或改使用方页面的 <link> 时，务必确认对方也引了 tokens.css。

   受影响页面（改任一页都要检查另一页）：
     /index.html  ·  /product/index.html  ·  /portal/index.html

   一键自检（服务器上执行，不依赖外网）：
     bash /opt/workbuddy/scripts/check_static_style.sh
   ============================================================ */"""

s = io.open(P, encoding='utf-8').read()
if '本文件单独无效' in s:
    print('  已存在，跳过')
else:
    if OLD in s:
        s = s.replace(OLD, NEW, 1)
        io.open(P, 'w', encoding='utf-8').write(s)
        print('  [OK] style.css 头部警示已强化')
    else:
        print('  [!] 未命中头部块，当前前 6 行：')
        for ln in s.split('\n')[:6]:
            print('     ', ln)
