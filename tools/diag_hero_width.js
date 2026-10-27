/**
 * Hero 两栏宽度与要点折行诊断。
 * 目标：量化「连接管理」条目为何折行，以及还能给左栏腾出多少水平空间。
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

  for (const w of [1280, 1440, 1920]) {
    const page = await browser.newPage({ viewport: { width: w, height: 900 } });
    await page.goto(FILE, { waitUntil: 'load' });
    await page.waitForTimeout(1200);

    const r = await page.evaluate(() => {
      const box = el => {
        if (!el) return null;
        const b = el.getBoundingClientRect();
        const cs = getComputedStyle(el);
        return {
          w: Math.round(b.width), h: Math.round(b.height),
          left: Math.round(b.left), right: Math.round(b.right),
          maxW: cs.maxWidth, gap: cs.gap,
          cols: cs.gridTemplateColumns,
        };
      };
      const split = document.querySelector('.hero-split');
      const copy = document.querySelector('.hero-copy');
      const shot = document.querySelector('.hero-shot');
      const points = document.querySelector('.hero-points');

      // 每条要点的行数与末行内容
      const items = Array.from(document.querySelectorAll('.hero-points li')).map(li => {
        const cs = getComputedStyle(li);
        const lh = parseFloat(cs.lineHeight) || 20;
        const b = li.getBoundingClientRect();
        const lines = Math.round(b.height / lh);
        // 找 <b> 之后文本节点的宽度需求
        return { text: li.textContent.trim().slice(0, 26), w: Math.round(b.width), h: Math.round(b.height), lh: Math.round(lh), lines };
      });

      // 测量「连接管理」整句在单行内所需宽度（用 canvas 量文本）
      const firstLi = document.querySelector('.hero-points li');
      let needW = 0;
      if (firstLi) {
        const style = getComputedStyle(firstLi);
        const cv = document.createElement('canvas');
        const ctx = cv.getContext('2d');
        ctx.font = `${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
        // 全句文本（不含竖标宽度与 gap）
        const fullText = firstLi.textContent.replace(/\s+/g, ' ').trim();
        needW = Math.ceil(ctx.measureText(fullText).width);
      }

      return {
        split: box(split), copy: box(copy), shot: box(shot), points: box(points),
        items, needW,
        wrapW: Math.round(document.querySelector('.hero-inner').getBoundingClientRect().width),
        docW: document.documentElement.clientWidth,
      };
    });

    console.log(`\n===== 视口 ${w}px =====`);
    console.log('  hero-inner 宽:', r.wrapW, '| split cols:', r.split.cols, '| gap:', r.split.gap);
    console.log('  左栏(hero-copy):', r.copy.w, 'px  left=' + r.copy.left, 'right=' + r.copy.right);
    console.log('  右栏(hero-shot):', r.shot.w, 'px  left=' + r.shot.left, 'right=' + r.shot.right);
    console.log('  .hero-points:', r.points.w, 'px  maxW=' + r.points.maxW);
    console.log('  「连接管理」整句单行所需文本宽:', r.needW, 'px');
    console.log('  要点折行情况:');
    r.items.forEach(it => console.log(`    ${it.lines} 行 | w=${it.w} | ${it.text}`));

    await page.close();
  }
  await browser.close();
})();
