/**
 * 下载弹窗交互验证 —— 覆盖：
 *   1. 点击「立即下载」弹出选择器（且不再跳锚点）
 *   2. 弹窗内两版本信息完整（大小 / 介绍 / 适用系统）
 *   3. 点击卡片可选中
 *   4. Esc / 遮罩 / 关闭按钮 三种关闭方式
 *   5. 从弹窗内点击下载按钮，真正拿到 PE exe（非 HTML）
 *   6. 页脚入口同样能打开弹窗
 * 用法：node verify_modal.js
 */
const { chromium } = require('playwright');
const fs = require('fs');
const os = require('os');
const path = require('path');

const PAGE = 'http://localhost:8080/';
const TMP = path.join(os.tmpdir(), 'xmtool-modal-verify');
fs.mkdirSync(TMP, { recursive: true });

let pass = 0, fail = 0;
function check(name, ok, extra) {
  if (ok) { pass++; console.log(`  ✅ ${name}${extra ? ' — ' + extra : ''}`); }
  else { fail++; console.log(`  ❌ ${name}${extra ? ' — ' + extra : ''}`); }
}

(async () => {
  const browser = await chromium.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
    args: ['--no-proxy-server', '--proxy-bypass-list=<-loopback>;127.0.0.1;10.*'],
    ignoreDefaultArgs: ['--proxy-server'],
  });
  const ctx = await browser.newContext({ acceptDownloads: true });
  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push('pageerror: ' + e.message));
  page.on('console', m => { if (m.type() === 'error') errors.push('console: ' + m.text()); });

  await page.goto(PAGE, { waitUntil: 'load' });
  await page.waitForTimeout(1200);

  console.log('=== 1) 打开弹窗 ===');
  const beforeUrl = page.url();
  await page.click('button[data-dl-open]');
  await page.waitForTimeout(500);
  const opened = await page.evaluate(() => {
    const m = document.getElementById('dlModal');
    return !!m && m.classList.contains('is-open') && !m.hidden;
  });
  check('点击「立即下载」弹出选择器', opened);
  check('未发生锚点跳转（URL 不变）', page.url() === beforeUrl, page.url() === beforeUrl ? '' : page.url());
  const heroVisible = await page.locator('header.hero').isVisible();
  check('弹窗打开后 Hero 仍存在（非整页替换）', heroVisible);
  const bodyLocked = await page.evaluate(() => document.body.classList.contains('modal-open'));
  check('背景滚动已锁定', bodyLocked);

  console.log('\n=== 2) 版本信息完整性 ===');
  const info = await page.evaluate(() => {
    return Array.from(document.querySelectorAll('#dlModal [data-dl-opt]')).map(el => {
      const name = el.querySelector('.dl-opt-name')?.textContent.trim();
      const size = (el.querySelector('.dl-opt-size .n')?.textContent.trim() || '') +
                   ' ' + (el.querySelector('.dl-opt-size .u')?.textContent.trim() || '');
      const desc = el.querySelector('.dl-opt-desc')?.textContent.trim();
      const specs = {};
      el.querySelectorAll('.dl-specs div').forEach(d => {
        const dt = d.querySelector('dt')?.textContent.trim();
        const dd = d.querySelector('dd')?.textContent.trim();
        if (dt) specs[dt] = dd;
      });
      const href = el.querySelector('a.dl-btn')?.href;
      const dlAttr = el.querySelector('a.dl-btn')?.hasAttribute('download');
      return { name, size, desc, specs, href, dlAttr };
    });
  });
  info.forEach(o => {
    console.log(`  [${o.name}] ${o.size}`);
    console.log(`     介绍: ${o.desc}`);
    Object.entries(o.specs).forEach(([k, v]) => console.log(`     ${k}: ${v}`));
    console.log(`     链接: ${o.href}  [download: ${o.dlAttr}]`);
  });
  check('恰好 2 个版本卡片', info.length === 2, `${info.length} 个`);
  info.forEach(o => {
    check(`「${o.name}」含文件大小`, /2\.5|70/.test(o.size), o.size);
    check(`「${o.name}」含简要介绍`, (o.desc || '').length > 12);
    check(`「${o.name}」含适用系统`, !!o.specs['适用系统'], o.specs['适用系统'] || '(缺)');
    check(`「${o.name}」含运行前提`, !!o.specs['运行前提']);
    check(`「${o.name}」链接为绝对路径`, (o.href || '').includes('/download/'));
    check(`「${o.name}」带 download 属性`, o.dlAttr === true);
  });

  console.log('\n=== 3) 选中交互 ===');
  const selFirst = await page.evaluate(() => {
    const els = document.querySelectorAll('#dlModal [data-dl-opt]');
    return els[0].classList.contains('is-sel');
  });
  check('打开时默认高亮推荐项', selFirst);
  await page.click('#dlModal [data-dl-opt="b"] .dl-opt-name');
  await page.waitForTimeout(200);
  const selB = await page.evaluate(() => {
    const els = document.querySelectorAll('#dlModal [data-dl-opt]');
    return { b: els[1].classList.contains('is-sel'), a: els[0].classList.contains('is-sel') };
  });
  check('点击第二项后选中态切换', selB.b === true && selB.a === false);

  console.log('\n=== 4) 三种关闭方式 ===');
  await page.keyboard.press('Escape');
  await page.waitForTimeout(450);
  check('Esc 关闭', await page.evaluate(() => !document.getElementById('dlModal').classList.contains('is-open')));
  check('关闭后背景滚动解锁', await page.evaluate(() => !document.body.classList.contains('modal-open')));
  const focusBack = await page.evaluate(() => document.activeElement?.hasAttribute('data-dl-open'));
  check('关闭后焦点还原到触发按钮', focusBack);

  await page.click('button[data-dl-open]');
  await page.waitForTimeout(400);
  await page.click('.dl-backdrop', { position: { x: 5, y: 5 } });
  await page.waitForTimeout(450);
  check('点击遮罩关闭', await page.evaluate(() => !document.getElementById('dlModal').classList.contains('is-open')));

  await page.click('button[data-dl-open]');
  await page.waitForTimeout(400);
  await page.click('.dl-close');
  await page.waitForTimeout(450);
  check('关闭按钮关闭', await page.evaluate(() => !document.getElementById('dlModal').classList.contains('is-open')));

  console.log('\n=== 5) 从弹窗内实际下载 ===');
  await page.click('button[data-dl-open]');
  await page.waitForTimeout(450);
  for (const key of ['a', 'b']) {
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 180000 }),
      page.click(`#dlModal [data-dl-opt="${key}"] a.dl-btn`),
    ]);
    const savePath = path.join(TMP, `modal_${key}.exe`);
    await download.saveAs(savePath);
    const buf = fs.readFileSync(savePath);
    const head = buf.slice(0, 2).toString('latin1');
    const isHTML = buf.slice(0, 200).toString('utf8').trim().startsWith('<');
    check(`弹窗内下载 ${key === 'a' ? '精简版' : '整合版'}`, head === 'MZ' && !isHTML,
      `${buf.length}B 头="${head}" PE=${head === 'MZ'} HTML=${isHTML}`);
  }

  console.log('\n=== 6) 页脚入口 ===');
  // 上一步下载后弹窗仍开着，先关闭再测页脚入口
  await page.keyboard.press('Escape');
  await page.waitForTimeout(500);
  await page.locator('.foot-cta').scrollIntoViewIfNeeded();
  await page.waitForTimeout(300);
  await page.click('.foot-cta');
  await page.waitForTimeout(500);
  check('页脚入口可打开弹窗', await page.evaluate(() => document.getElementById('dlModal').classList.contains('is-open')));
  await page.keyboard.press('Escape');

  console.log('\n=== 7) JS 错误 ===');
  console.log(errors.length ? errors.join('\n') : '无');

  console.log(`\n=== 汇总：通过 ${pass} / 失败 ${fail} ===`);
  await browser.close();
  fs.rmSync(TMP, { recursive: true, force: true });
  process.exit(fail === 0 ? 0 : 1);
})();
