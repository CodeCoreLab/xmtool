/**
 * 线上 hero 排版实测：断言四条要点均单行完整，并检查截图渲染。
 * 只看数值，不靠截图肉眼。
 */
const { chromium } = require('playwright');

const ONLINE = 'http://localhost:8080/';

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });

  let pass = 0, fail = 0;
  const chk = (n, ok, ex) => { ok ? (pass++, console.log(`  ✅ ${n}${ex ? ' — ' + ex : ''}`)) : (fail++, console.log(`  ❌ ${n}${ex ? ' — ' + ex : ''}`)); };

  for (const w of [1280, 1440, 1920]) {
    console.log(`\n===== 线上 ${w}px =====`);
    const page = await browser.newPage({ viewport: { width: w, height: 900 } });
    await page.goto(ONLINE, { waitUntil: 'load' });
    await page.waitForTimeout(1800);

    const r = await page.evaluate(() => {
      const items = Array.from(document.querySelectorAll('.hero-points li')).map(li => {
        const cs = getComputedStyle(li);
        const lh = parseFloat(cs.lineHeight) || 20;
        const b = li.getBoundingClientRect();
        return {
          text: li.textContent.trim().slice(0, 20),
          w: Math.round(b.width), h: Math.round(b.height),
          lh: Math.round(lh), lines: Math.round(b.height / lh),
        };
      });
      const copy = document.querySelector('.hero-copy');
      const shot = document.querySelector('.hero-shot');
      const img = document.querySelector('.hero-shot-frame img');
      // 首条要点末字是否被孤立：比较文本节点末行
      const first = document.querySelector('.hero-points li');
      let lastCharAlone = false;
      if (first) {
        const range = document.createRange();
        range.selectNodeContents(first);
        const rects = Array.from(range.getClientRects());
        const liRect = first.getBoundingClientRect();
        // 行数 > 期望值即说明折行
        lastCharAlone = Math.round(first.getBoundingClientRect().height / (parseFloat(getComputedStyle(first).lineHeight) || 20)) > 1;
      }
      return {
        items,
        copyW: Math.round(copy.getBoundingClientRect().width),
        shotW: Math.round(shot.getBoundingClientRect().width),
        imgNatW: img ? img.naturalWidth : 0,
        imgW: img ? Math.round(img.getBoundingClientRect().width) : 0,
        overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        lastCharAlone,
      };
    });

    chk('四条要点全部单行', r.items.length === 4 && r.items.every(i => i.lines === 1),
      r.items.map(i => i.lines).join('/'));
    chk('「连接管理」条目单行（末字不孤立）', !r.lastCharAlone, r.items[0]?.text);
    chk('界面截图已加载', r.imgNatW > 0, `${r.imgNatW}px 原始 → ${r.imgW}px 渲染`);
    chk('无横向溢出', r.overflow === 0, r.overflow + 'px');
    console.log(`  左栏 ${r.copyW}px | 右栏 ${r.shotW}px`);

    await page.close();
  }

  console.log(`\n=== 汇总：通过 ${pass} / 失败 ${fail} ===`);
  await browser.close();
  process.exit(fail === 0 ? 0 : 1);
})();
