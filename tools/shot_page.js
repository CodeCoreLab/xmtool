const { chromium } = require('playwright');
const path = require('path');
const FILE = 'file:///' + path.resolve(__dirname, '..', 'docs', '小马桌面工具-产品页.html').replace(/\\/g, '/');
const OUT = path.resolve(__dirname, '..', 'docs', '_verify-screenshots') + path.sep;

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const shots = [
    ['hero', 1440, 900, 0],
    ['features', 1440, 1100, 2100],
    ['advantage', 1440, 1100, 5900],
    ['release', 1440, 1250, 8300],
    ['mobile-hero', 390, 844, 0],
    ['mobile-arch', 390, 900, 3200],
  ];
  for (const [name, w, h, y] of shots) {
    const page = await browser.newPage({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
    await page.goto(FILE, { waitUntil: 'load' });
    await page.waitForTimeout(700);
    // 先滚一遍触发 reveal + 粒子可见
    await page.evaluate(async (target) => {
      const step = window.innerHeight * 0.75;
      for (let ty = 0; ty <= target + window.innerHeight; ty += step) {
        window.scrollTo(0, ty); await new Promise(r => setTimeout(r, 130));
      }
      window.scrollTo(0, target);
    }, y);
    await page.waitForTimeout(1100);
    await page.screenshot({ path: OUT + name + '.png', fullPage: false });
    await page.close();
  }
  await browser.close();
  console.log('shots saved');
})();
