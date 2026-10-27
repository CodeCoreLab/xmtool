/**
 * Aireport 根首页样式生效验证。
 * 判据：CSS 变量是否解析出实际颜色值（而非空），关键区块背景色是否非纯白。
 */
const { chromium } = require('playwright');

const URL = 'http://localhost:8080/';

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const failed = [];
  page.on('requestfailed', r => failed.push(r.url()));
  const statuses = [];
  page.on('response', r => {
    if (/\.(css|js)(\?|$)/.test(r.url())) statuses.push({ url: r.url(), st: r.status() });
  });

  await page.goto(URL, { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);

  const r = await page.evaluate(() => {
    const cs = getComputedStyle(document.documentElement);
    const vars = ['--brand', '--brand-deep', '--n-ink', '--n-body', '--sev-p0', '--brand-soft'];
    const resolved = {};
    vars.forEach(v => resolved[v] = cs.getPropertyValue(v).trim() || '(空)');

    // 关键区块的实际计算样式
    const pick = sel => {
      const el = document.querySelector(sel);
      if (!el) return null;
      const s = getComputedStyle(el);
      return { bg: s.backgroundColor, color: s.color, display: s.display };
    };

    return {
      resolved,
      body: pick('body'),
      nav: pick('.nav'),
      hero: pick('.hero, .hero-section, section'),
      progress: pick('.progress-bar'),
      cards: document.querySelectorAll('.card, .stat, .metric').length,
      // 统计：有多少元素的背景是纯白（rgb(255,255,255)）
      whiteBgCount: Array.from(document.querySelectorAll('body *')).filter(el => {
        const s = getComputedStyle(el);
        return s.backgroundColor === 'rgb(255, 255, 255)' && el.offsetHeight > 50;
      }).length,
      bodyBg: getComputedStyle(document.body).backgroundColor,
      htmlBg: getComputedStyle(document.documentElement).backgroundColor,
    };
  });

  console.log('=== ① 资源加载状态 ===');
  statuses.forEach(s => console.log(`  ${s.st}  ${s.url.replace('http://localhost:8080', '')}`));
  console.log('  失败请求:', failed.length ? failed.join(', ') : '无');

  console.log('\n=== ② CSS 变量解析（应有实际色值）===');
  let allOk = true;
  Object.entries(r.resolved).forEach(([k, v]) => {
    const ok = v !== '(空)';
    if (!ok) allOk = false;
    console.log(`  ${ok ? '✅' : '❌'} ${k} = ${v}`);
  });

  console.log('\n=== ③ 关键区域计算样式 ===');
  console.log('  html  bg:', r.htmlBg);
  console.log('  body  bg:', r.bodyBg);
  console.log('  nav  :', JSON.stringify(r.nav));
  console.log('  区块数(card/stat/metric):', r.cards);
  console.log('  大块纯白背景元素数:', r.whiteBgCount);

  console.log('\n=== 结论 ===');
  console.log('  CSS 变量全解析:', allOk ? '✅ 是（样式已生效）' : '❌ 否（仍有未定义变量）');

  await page.screenshot({ path: './repo/docs/_verify-screenshots/aireport-root-fixed.png' });
  await browser.close();
})();
