/**
 * 界面截图 + 灯箱验证：
 *   ① hero 右侧截图加载成功（naturalWidth > 0）
 *   ② 图片实际渲染尺寸合理（未被压扁/拉伸）
 *   ③ 点击截图打开灯箱、显示大图
 *   ④ Esc / 关闭按钮 / 点背景 三种关闭方式
 *   ⑤ dl-note 提示语已从页面正文移除（仅在 HTML 注释中）
 *   ⑥ 桌面三档无横向溢出
 */
const { chromium } = require('playwright');
const path = require('path');

const ONLINE = 'http://localhost:8080/';
const LOCAL = 'file:///' + path.resolve(__dirname, '..', 'docs', '小马桌面工具-产品页.html').replace(/\\/g, '/');

let pass = 0, fail = 0;
const chk = (n, ok, ex) => { ok ? (pass++, console.log(`  ✅ ${n}${ex ? ' — ' + ex : ''}`)) : (fail++, console.log(`  ❌ ${n}${ex ? ' — ' + ex : ''}`)); };

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });

  // ── A. 线上验证 ─────────────────────────────────────────────────
  console.log('=== A. 线上页面（' + ONLINE + '）===');
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errs = [];
  page.on('pageerror', e => errs.push('pageerror: ' + e.message));
  page.on('console', m => { if (m.type() === 'error') errs.push('console: ' + m.text()); });
  await page.goto(ONLINE, { waitUntil: 'load' });
  await page.waitForTimeout(2200);

  const imgInfo = await page.evaluate(() => {
    const i = document.querySelector('.hero-shot-frame img');
    if (!i) return null;
    const r = i.getBoundingClientRect();
    return {
      src: i.getAttribute('src'),
      natW: i.naturalWidth, natH: i.naturalHeight,
      cssW: Math.round(r.width), cssH: Math.round(r.height),
      complete: i.complete,
      alt: i.getAttribute('alt'),
    };
  });
  chk('hero 右侧存在界面截图', !!imgInfo, imgInfo ? imgInfo.src : '');
  if (imgInfo) {
    chk('图片加载成功（naturalWidth>0）', imgInfo.natW > 0, `${imgInfo.natW}x${imgInfo.natH}`);
    chk('渲染尺寸合理（宽高比≈1.39）',
      Math.abs((imgInfo.cssW / imgInfo.cssH) - (imgInfo.natW / imgInfo.natH)) < 0.1,
      `${imgInfo.cssW}x${imgInfo.cssH}`);
    chk('含描述性 alt', (imgInfo.alt || '').length > 10);
  }

  // 提示语已移除（页面可见文本中不应出现）
  const bodyText = await page.evaluate(() => document.body.innerText);
  chk('「下载地址为固定槽位…」已不在正文', !bodyText.includes('下载地址为固定槽位'));
  const hasComment = await page.evaluate(async () => {
    const r = await fetch(location.href);
    const t = await r.text();
    return t.includes('运维备注（不对外展示）') && t.includes('download/archive');
  });
  chk('信息保留在 HTML 注释中', hasComment);

  // 灯箱交互
  console.log('\n=== B. 截图灯箱 ===');
  await page.click('.hero-shot-frame');
  await page.waitForTimeout(500);
  chk('点击截图打开灯箱', await page.evaluate(() => {
    const l = document.getElementById('shotLightbox');
    return l && l.classList.contains('is-open') && !l.hidden;
  }));
  const lbImg = await page.evaluate(() => {
    const i = document.getElementById('shotLightboxImg');
    return { src: i.getAttribute('src'), natW: i.naturalWidth, maxH: getComputedStyle(i).maxHeight };
  });
  chk('灯箱内大图已加载', lbImg.natW > 0, lbImg.src);
  chk('背景滚动已锁定', await page.evaluate(() => document.body.classList.contains('modal-open')));

  await page.keyboard.press('Escape');
  await page.waitForTimeout(500);
  chk('Esc 关闭灯箱', await page.evaluate(() => !document.getElementById('shotLightbox').classList.contains('is-open')));

  await page.click('.hero-shot-frame');
  await page.waitForTimeout(450);
  await page.click('.shot-lightbox-close');
  await page.waitForTimeout(500);
  chk('关闭按钮关闭灯箱', await page.evaluate(() => !document.getElementById('shotLightbox').classList.contains('is-open')));

  await page.click('.hero-shot-frame');
  await page.waitForTimeout(450);
  await page.mouse.click(30, 30);
  await page.waitForTimeout(500);
  chk('点背景关闭灯箱', await page.evaluate(() => !document.getElementById('shotLightbox').classList.contains('is-open')));

  // 下载弹窗仍正常（回归）
  await page.click('button[data-dl-open]');
  await page.waitForTimeout(450);
  chk('下载弹窗功能未受影响', await page.evaluate(() => document.getElementById('dlModal').classList.contains('is-open')));
  await page.keyboard.press('Escape');

  console.log('\nJS 错误:', errs.length ? errs.join('\n') : '无');
  if (errs.length) fail++;
  await page.close();

  // ── C. 桌面三档无溢出 ───────────────────────────────────────────
  console.log('\n=== C. 桌面三档（本地） ===');
  for (const [w, h, n] of [[1920, 1080, '1920'], [1440, 900, '1440'], [1280, 800, '1280']]) {
    const p2 = await browser.newPage({ viewport: { width: w, height: h } });
    // 本地用 file:// 时绝对路径 /download/... 不可达，仅检查布局
    await p2.goto(LOCAL, { waitUntil: 'load' });
    await p2.waitForTimeout(1200);
    await p2.evaluate(async () => {
      const s = innerHeight * 0.6;
      for (let y = 0; y < document.body.scrollHeight; y += s) { scrollTo(0, y); await new Promise(r => setTimeout(r, 200)); }
      scrollTo(0, 0);
    });
    await p2.waitForTimeout(500);
    const ov = await p2.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    chk(`${n}px 无横向溢出`, ov === 0, ov + 'px');
    await p2.close();
  }

  console.log(`\n=== 汇总：通过 ${pass} / 失败 ${fail} ===`);
  await browser.close();
  process.exit(fail === 0 ? 0 : 1);
})();
