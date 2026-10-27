/**
 * 按钮精度诊断：测量 padding / font / 图标 / 文字实际宽度，
 * 判断按钮为何宽度不足（桌面端 1440 视角）。
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
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto(FILE, { waitUntil: 'load' });
  await page.waitForTimeout(800);

  const r = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('.closing-cta .btn, .hero-cta .btn').forEach(b => {
      const cs = getComputedStyle(b);
      const rect = b.getBoundingClientRect();
      // 文字节点单独包一个 span 测宽：这里用 Range 直接量文本
      const tn = Array.from(b.childNodes).find(n => n.nodeType === 3 && n.textContent.trim());
      let textW = null;
      if (tn) {
        const rg = document.createRange();
        rg.selectNodeContents(tn);
        textW = Math.round(rg.getBoundingClientRect().width);
      }
      const svg = b.querySelector('svg');
      const svgW = svg ? Math.round(svg.getBoundingClientRect().width) : 0;
      out.push({
        text: b.textContent.trim(),
        boxW: Math.round(rect.width), boxH: Math.round(rect.height),
        padL: cs.paddingLeft, padR: cs.paddingRight,
        gap: cs.gap, fontSize: cs.fontSize,
        textW, svgW,
        needW: (textW || 0) + svgW + (svgW ? parseFloat(cs.gap || 0) : 0) + parseFloat(cs.paddingLeft) + parseFloat(cs.paddingRight),
        overflow: (tn ? (textW > 0 && b.scrollWidth > b.clientWidth) : false),
        scrollW: b.scrollWidth, clientW: b.clientWidth,
        whiteSpace: cs.whiteSpace, flex: cs.flex,
        container: b.parentElement.className.split(' ')[0],
      });
    });
    return out;
  });

  r.forEach(x => {
    console.log(`「${x.text}」 @${x.container}`);
    console.log(`   盒宽=${x.boxW}  内容需=${x.needW}  ${x.boxW < x.needW ? '❌ 不足 ' + (x.needW - x.boxW) + 'px' : '✅ 足够'}`);
    console.log(`   padding=${x.padL}/${x.padR}  gap=${x.gap}  font=${x.fontSize}`);
    console.log(`   文字宽=${x.textW} 图标宽=${x.svgW}  ws=${x.whiteSpace} flex=${x.flex}`);
    console.log(`   scroll=${x.scrollW} client=${x.clientW} ${x.scrollW > x.clientW ? '⚠️ 内容溢出' : ''}`);
    console.log('');
  });
  await browser.close();
})();
