const { chromium } = require('playwright');
const path = require('path');
const FILE = 'file:///' + path.resolve(__dirname, '..', 'docs', '小马桌面工具-产品页.html').replace(/\\/g, '/');

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await page.goto(FILE, { waitUntil: 'load' });
  await page.waitForTimeout(800);

  const diag = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('canvas.section-particles').forEach((c, i) => {
      const r = c.getBoundingClientRect();
      const sec = c.closest('section, .hero, footer');
      out.push({
        i, scene: c.dataset.scene,
        secTop: Math.round(sec ? sec.offsetTop : -1),
        cssW: Math.round(r.width), cssH: Math.round(r.height),
        attrW: c.width, attrH: c.height,
        secPadTop: sec ? getComputedStyle(sec).paddingTop : '',
      });
    });
    // 找出最大宽度的元素
    let widest = null, max = 0;
    document.querySelectorAll('*').forEach(el => {
      const w = el.scrollWidth;
      if (w > max && el.getBoundingClientRect().width > 0) { max = w; widest = el; }
    });
    return {
      canvases: out,
      widest: widest ? { tag: widest.tagName, cls: widest.className, w: Math.round(widest.getBoundingClientRect().width), sw: widest.scrollWidth } : null,
      docScrollW: document.documentElement.scrollWidth,
      bodyScrollW: document.body.scrollWidth,
    };
  });
  console.log(JSON.stringify(diag, null, 1));
  await browser.close();
})();
