# -*- coding: utf-8 -*-
"""HTML 结构完整性检查：--i 形态、标签配对、动效类分布。"""
import io
import re

P = r'E:\xmtool\xmtool\docs\小马桌面工具-产品页.html'
s = io.open(P, encoding='utf-8').read()

ok = re.findall(r'--i:\d+"', s)
all_i = re.findall(r'--i:[^"\s]*', s)
bad = [v for v in all_i if not re.fullmatch(r'--i:\d+', v)]

print('正确的 --i 值数量:', len(ok))
print('异常 --i 值:', bad[:5] if bad else '无')

print('div 配对:', len(re.findall(r'<div[ >]', s)), '/', len(re.findall(r'</div>', s)))
print('section 配对:', len(re.findall(r'<section[ >]', s)), '/', len(re.findall(r'</section>', s)))
print('script 配对:', len(re.findall(r'<script', s)), '/', len(re.findall(r'</script>', s)))
print('style 配对:', len(re.findall(r'<style', s)), '/', len(re.findall(r'</style>', s)))

print()
print('动效类分布:')
for cls in ['rise', 'cards', 'stack', 'rows', 'digits', 'slide', 'scale', 'lines']:
    print('  reveal-%-8s %d' % (cls, len(re.findall('class="[^"]*reveal-' + cls, s))))
