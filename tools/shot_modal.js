/** 弹窗视觉截图（桌面 + 移动端） */
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');
const { resolveChromePath } = require('./chrome_path');

const PAGE = 'http://localhost:8080/';
const OUT = path.resolve(__dirname, '..', 'docs', '_verify-screenshots') + path.sep;

(async () => {
  const browser = await chromium.launch({
    executablePath: resolveChromePath(),   // A42：不再硬编码本机 Chrome 路径
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const shots = [
    ['modal-desktop', 1280, 900, 0],
    ['modal-mobile', 390, 844, 0],
  ];
  for (const [name, w, h] of shots) {
    const page = await browser.newPage({ viewport: { width: w, height: h }, deviceScaleFactor: 1 });
    await page.goto(PAGE, { waitUntil: 'load' });
    await page.waitForTimeout(1000);
    await page.click('button[data-dl-open]');
    await page.waitForTimeout(900);
    await page.screenshot({ path: OUT + name + '.png', fullPage: false });
    await page.close();
  }
  await browser.close();
  console.log('modal shots saved');
})();
