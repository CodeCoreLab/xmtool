/**
 * 布局诊断：测量关键容器与按钮的实际几何尺寸，定位「文字竖排」根因，
 * 并做全页对齐审计（左边缘 / 宽度异常 / 内容溢出）。
 * 用法：node diag_layout.js
 */
const { chromium } = require('playwright');
const path = require('path');

const FILE = 'file:///' + path.resolve(__dirname, '..', 'docs', '小马桌面工具-产品页.html').replace(/\\/g, '/');

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 });
  await page.goto(FILE, { waitUntil: 'load' });
  await page.waitForTimeout(900);
  // 触发所有 reveal，让布局稳定
  await page.evaluate(async () => {
    const step = window.innerHeight * 0.55;
    for (let y = 0; y < document.body.scrollHeight; y += step) {
      window.scrollTo(0, y); await new Promise(r => setTimeout(r, 260));
    }
    window.scrollTo(0, 0);
  });
  await page.waitForTimeout(600);

  const result = await page.evaluate(() => {
    const m = el => {
      if (!el) return null;
      const r = el.getBoundingClientRect();
      const cs = getComputedStyle(el);
      return {
        w: Math.round(r.width), h: Math.round(r.height),
        display: cs.display, flex: cs.flex, minW: cs.minWidth,
        whiteSpace: cs.whiteSpace, justifyContent: cs.justifyContent,
        alignItems: cs.alignItems, writingMode: cs.writingMode,
      };
    };

    // 1) 收尾段按钮链
    const chain = {
      closing: m(document.querySelector('.closing')),
      inner: m(document.querySelector('.closing-inner')),
      cta: m(document.querySelector('.closing-cta')),
    };
    const btns = Array.from(document.querySelectorAll('.closing-cta .btn')).map(b => {
      const g = m(b);
      // 文字是否换行：clientHeight 远大于单行高度
      const lh = parseFloat(getComputedStyle(b).lineHeight) || 24;
      return { text: b.textContent.trim(), ...g, textWrap: g.h > (lh + 12) };
    });

    // 2) hero 按钮（对照组）
    const heroBtns = Array.from(document.querySelectorAll('.hero-cta .btn')).map(b => ({
      text: b.textContent.trim(), ...m(b),
    }));

    // 3) 全页对齐审计：找出与容器左边缘不对齐的卡片/标题
    const wraps = Array.from(document.querySelectorAll('.wrap'));
    const misaligned = [];
    const containers = Array.from(document.querySelectorAll('.grid,.release,.scene-grid,.arch-stack,.quality-grid,.dl-opts'));
    containers.forEach(c => {
      const cr = c.getBoundingClientRect();
      Array.from(c.children).forEach(ch => {
        const br = ch.getBoundingClientRect();
        const delta = Math.abs(br.left - cr.left);
        if (delta > 2 && getComputedStyle(c).display !== 'grid') {
          misaligned.push({
            container: c.className.slice(0, 40),
            child: (ch.className || '').slice(0, 40),
            deltaLeft: Math.round(delta),
          });
        }
      });
    });

    // 4) 横向溢出 / 内容被裁切
    const overflowers = [];
    document.querySelectorAll('*').forEach(el => {
      if (el.scrollWidth > el.clientWidth + 4 && el.clientWidth > 0) {
        const cs = getComputedStyle(el);
        if (cs.overflowX === 'auto' || cs.overflowX === 'scroll' || cs.overflow === 'auto') return; // 有意滚动
        overflowers.push({
          tag: el.tagName, cls: (el.className || '').toString().slice(0, 40),
          scrollW: el.scrollWidth, clientW: el.clientWidth,
        });
      }
    });

    return {
      chain, btns, heroBtns,
      misaligned: misaligned.slice(0, 15),
      overflowers: overflowers.slice(0, 15),
      docOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    };
  });

  console.log('=== 1) 收尾段容器链 ===');
  for (const [k, v] of Object.entries(result.chain)) {
    if (v) console.log(`  ${k.padEnd(8)} w=${v.w} h=${v.h} display=${v.display} justify=${v.justifyContent} align=${v.alignItems}`);
  }
  console.log('\n=== 2) 收尾段按钮 ===');
  result.btns.forEach(b => console.log(`  「${b.text}」 w=${b.w} h=${b.h} flex=${b.flex} minW=${b.minW} ws=${b.whiteSpace} 文字竖排=${b.textWrap}`));
  console.log('\n=== 3) Hero 按钮（对照）===');
  result.heroBtns.forEach(b => console.log(`  「${b.text}」 w=${b.w} h=${b.h} flex=${b.flex} ws=${b.whiteSpace}`));
  console.log('\n=== 4) 对齐审计（子元素偏离容器左缘）===');
  if (!result.misaligned.length) console.log('  无');
  else result.misaligned.forEach(x => console.log(`  ${x.container} → ${x.child}  偏离 ${x.deltaLeft}px`));
  console.log('\n=== 5) 内容溢出（非滚动容器）===');
  if (!result.overflowers.length) console.log('  无');
  else result.overflowers.forEach(x => console.log(`  <${x.tag} class="${x.cls}"> scroll=${x.scrollW} client=${x.clientW}`));
  console.log('\n文档横向溢出:', result.docOverflow, 'px');

  await browser.close();
})();
