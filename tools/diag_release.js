/**
 * 定位发布卡片中「数字右侧横条」的真实来源。
 * 策略：列出 #release 内所有元素（含伪元素）的几何与背景，筛选出疑似细长横条。
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
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  await page.goto(FILE, { waitUntil: 'load' });
  await page.waitForTimeout(900);
  await page.evaluate(async () => {
    const s = window.innerHeight * 0.55;
    for (let y = 0; y < document.body.scrollHeight; y += s) { scrollTo(0, y); await new Promise(r => setTimeout(r, 200)); }
  });
  const el = page.locator('#release');
  await el.scrollIntoViewIfNeeded();
  await page.waitForTimeout(600);

  const data = await page.evaluate(() => {
    const rel = document.querySelector('#release');
    const relRect = rel.getBoundingClientRect();
    const toRel = b => ({ l: Math.round(b.left - relRect.left), t: Math.round(b.top - relRect.top), w: Math.round(b.width), h: Math.round(b.height) });

    const els = [];
    rel.querySelectorAll('*').forEach(el => {
      const b = el.getBoundingClientRect();
      if (b.width < 1 || b.height < 1) return;
      const cs = getComputedStyle(el);
      els.push({
        tag: el.tagName, cls: (el.className || '').toString().slice(0, 34),
        ...toRel(b),
        bg: cs.backgroundColor,
        bgi: cs.backgroundImage !== 'none' ? cs.backgroundImage.slice(0, 46) : 'none',
        pos: cs.position, op: cs.opacity,
      });
    });

    // 伪元素
    const pseudo = [];
    rel.querySelectorAll('*').forEach(el => {
      ['::before', '::after'].forEach(p => {
        const cs = getComputedStyle(el, p);
        if (cs.content && cs.content !== 'none' && cs.content !== 'normal') {
          const b = el.getBoundingClientRect();
          pseudo.push({
            sel: el.tagName + '.' + (el.className || '').toString().slice(0, 22) + p,
            content: cs.content.slice(0, 24),
            w: cs.width, h: cs.height, pos: cs.position,
            bg: cs.background.slice(0, 50),
            rect: toRel(b),
          });
        }
      });
    });

    // 数字元素的精确框
    const nums = Array.from(rel.querySelectorAll('.rel-size')).map(n => {
      const b = n.getBoundingClientRect();
      const cs = getComputedStyle(n);
      return { rect: toRel(b), display: cs.display, bgClip: cs.webkitBackgroundClip || cs.backgroundClip, fill: cs.webkitTextFillColor };
    });

    // 徽标位置
    const badges = Array.from(rel.querySelectorAll('.rel-badge')).map(n => {
      const b = n.getBoundingClientRect();
      const cs = getComputedStyle(n);
      return { rect: toRel(b), pos: cs.position, left: cs.left, right: cs.right, top: cs.top, parentPos: getComputedStyle(n.parentElement).position };
    });

    return { els, pseudo, nums, badges, relH: Math.round(relRect.height) };
  });

  console.log('=== .rel-size（数字） ===');
  data.nums.forEach(n => console.log('  ', JSON.stringify(n)));
  console.log('\n=== .rel-badge（徽标） ===');
  data.badges.forEach(n => console.log('  ', JSON.stringify(n)));

  console.log('\n=== 伪元素（content 非 none） ===');
  if (!data.pseudo.length) console.log('   无');
  else data.pseudo.forEach(p => console.log('  ', JSON.stringify(p)));

  console.log('\n=== 疑似横条（高度 < 30 且宽度 > 80，有背景） ===');
  const bars = data.els.filter(e => e.h < 30 && e.w > 80 && e.bg !== 'rgba(0, 0, 0, 0)' && e.op !== '0');
  if (!bars.length) console.log('   无');
  else bars.forEach(b => console.log('  ', JSON.stringify(b)));

  console.log('\n=== 数字所在垂直带内的全部元素（|Δt| < 30） ===');
  const num0 = data.nums[0] ? data.nums[0].rect : null;
  if (num0) {
    const band = data.els.filter(e => Math.abs(e.t - num0.t) < 30);
    band.forEach(b => console.log('  ', JSON.stringify(b)));
  }

  await browser.close();
})();
