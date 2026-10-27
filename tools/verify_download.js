/**
 * 浏览器级下载实测：真正点击页面上的下载按钮，断言拿到的是 PE 可执行文件而非 HTML。
 * 这是本次「下载文件不正确」问题的关键验证方式 —— curl 直接测 URL 无法暴露相对路径解析错误。
 */
const { chromium } = require('playwright');
const fs = require('fs');
const os = require('os');
const path = require('path');

const PAGE = 'http://localhost:8080/';
const TMP = path.join(os.tmpdir(), 'xmtool-dl-verify');
fs.mkdirSync(TMP, { recursive: true });

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

  // 下载入口已改为弹窗：先点「立即下载」打开选择器，再取弹窗内的按钮
  await page.click('button[data-dl-open]');
  await page.waitForTimeout(600);

  // 检查链接的解析后绝对地址
  const hrefs = await page.$$eval('#dlModal a.dl-btn', els => els.map(e => ({ text: e.textContent.trim(), abs: e.href, dl: e.hasAttribute('download') })));
  console.log('=== 下载链接解析（弹窗内） ===');
  hrefs.forEach(h => console.log(`  「${h.text}」→ ${h.abs}  [download属性: ${h.dl}]`));

  console.log('\n=== 实际点击下载 ===');
  for (let i = 0; i < hrefs.length; i++) {
    const sel = `#dlModal a.dl-btn >> nth=${i}`;
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 120000 }),
      page.click(sel),
    ]);
    const fname = download.suggestedFilename();
    const savePath = path.join(TMP, `dl${i}_${fname}`);
    await download.saveAs(savePath);
    const buf = fs.readFileSync(savePath);
    const head2 = buf.slice(0, 2).toString('latin1');
    const isPE = head2 === 'MZ';
    const isHTML = buf.slice(0, 200).toString('utf8').trim().startsWith('<');
    console.log(`  「${hrefs[i].text}」→ ${fname}`);
    console.log(`     大小 ${buf.length} B ｜ 头2字节 "${head2}" ｜ PE=${isPE} ｜ 是HTML=${isHTML}`);
    if (!isPE) { console.log('     ❌ 不是有效的 PE 可执行文件'); }
    else { console.log('     ✅ 有效 exe'); }
  }

  console.log('\n=== JS 错误 ===');
  console.log(errors.length ? errors.join('\n') : '无');
  await browser.close();
  // 清理临时下载
  fs.rmSync(TMP, { recursive: true, force: true });
  console.log('\n临时文件已清理: ' + TMP);
})();
