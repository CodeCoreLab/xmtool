/**
 * 产品页渲染验证：无 JS 报错 + 各画布确实绘制了像素 + 三档视口布局不溢出。
 * 用法（需经代理注入，见下）：
 *   node verify_page.js
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
  const errors = [];
  const viewports = [[1920, 1080, 'full-hd'], [1440, 900, 'laptop'], [1280, 800, 'small-desktop']];
  let out = [];

  for (const [w, h, name] of viewports) {
    const page = await browser.newPage({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
    page.on('pageerror', e => errors.push(`[${name}] pageerror: ${e.message}`));
    page.on('console', m => { if (m.type() === 'error') errors.push(`[${name}] console: ${m.text()}`); });
    await page.goto(FILE, { waitUntil: 'load' });
    await page.waitForTimeout(1200);
    // 滚到底把所有 section 触发一遍
    await page.evaluate(async () => {
      const step = window.innerHeight * 0.55;
      for (let y = 0; y < document.body.scrollHeight; y += step) {
        window.scrollTo(0, y); await new Promise(r => setTimeout(r, 420));
      }
      window.scrollTo(0, 0);
    });
    await page.waitForTimeout(900);

      const stats = await page.evaluate(() => {
      const res = [];
      document.querySelectorAll('canvas.section-particles').forEach((c, i) => {
        const ctx = c.getContext('2d');
        const { width: cw, height: ch } = c;
        if (!cw || !ch) { res.push({ i, ok: false, reason: 'zero-size' }); return; }
        // 按画布中心取样（orbit/converge 的粒子集中在中心区域）
        const sw = Math.min(cw, 520), sh = Math.min(ch, 340);
        const sx = Math.round((cw - sw) / 2), sy = Math.round((ch - sh) / 2);
        let data;
        try { data = ctx.getImageData(sx, sy, sw, sh).data; }
        catch (e) { res.push({ i, ok: false, reason: 'getImageData-failed' }); return; }
        let painted = 0;
        for (let k = 3; k < data.length; k += 4) if (data[k] > 6) painted++;
        res.push({ i, scene: c.dataset.scene, painted, ratio: +(painted / (data.length / 4)).toFixed(4) });
      });
      return {
        canvases: res,
        hOverflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
        revealsIn: document.querySelectorAll('.reveal.in').length,
        revealsTotal: document.querySelectorAll('.reveal').length,
        sections: document.querySelectorAll('section, .hero, footer').length,
      };
    });

    out.push(`--- ${name} ${w}x${h} ---`);
    out.push(`  canvases: ${stats.canvases.length}, sections: ${stats.sections}`);
    out.push(`  reveals: ${stats.revealsIn}/${stats.revealsTotal} in`);
    out.push(`  水平溢出: ${stats.hOverflow}px`);
    const dead = stats.canvases.filter(c => !('painted' in c) || c.painted === 0);
    out.push(`  未绘制画布: ${dead.length === 0 ? '无' : JSON.stringify(dead)}`);
    out.push(`  绘制明细: ${stats.canvases.map(c => `#${c.i}(${c.scene || '?'})=${c.painted ?? c.reason}`).join(' ')}`);
    await page.close();
  }

  await browser.close();
  console.log(out.join('\n'));
  console.log('\n=== errors ===');
  console.log(errors.length ? errors.join('\n') : '无 JS 报错');
})();
