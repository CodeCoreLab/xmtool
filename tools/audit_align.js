/**
 * 桌面端对齐精度测量（1440 视口）—— 用数值而非肉眼判断。
 * 检查：居中偏差 / 左边缘基线 / 同行卡片高度差 / 网格列宽差。
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
  await page.evaluate(async () => {
    const s = window.innerHeight * 0.55;
    for (let y = 0; y < document.body.scrollHeight; y += s) {
      window.scrollTo(0, y); await new Promise(r => setTimeout(r, 220));
    }
    window.scrollTo(0, 0);
  });
  await page.waitForTimeout(600);

  const r = await page.evaluate(() => {
    const rect = el => el.getBoundingClientRect();
    const midX = el => { const b = rect(el); return Math.round(b.left + b.width / 2); };
    const pageMid = Math.round(document.documentElement.clientWidth / 2);
    const probs = [];

    // ① 收尾段按钮组居中
    const cta = document.querySelector('.closing-cta');
    const ctaMid = midX(cta);
    if (Math.abs(ctaMid - pageMid) > 4) {
      probs.push({ lv: 'P1', what: '收尾段按钮组未居中', d: `中心=${ctaMid} 应为=${pageMid} 偏差=${ctaMid - pageMid}px` });
    }

    // ② 各栏目标题居中检查（closing 段为居中排版）
    const closingH2 = document.querySelector('.closing h2');
    if (closingH2 && Math.abs(midX(closingH2) - pageMid) > 4) {
      probs.push({ lv: 'P2', what: '收尾段标题未居中', d: `中心=${midX(closingH2)} 应为=${pageMid}` });
    }

    // ③ 左边缘基线统一（所有 .wrap 与 .head 应与 wrap 同左）
    const wraps = Array.from(document.querySelectorAll('.wrap'));
    const lefts = wraps.map(w => Math.round(rect(w).left));
    const uniq = [...new Set(lefts)];
    if (uniq.length > 1) probs.push({ lv: 'P1', what: '.wrap 左边缘不一致', d: uniq.join(' / ') });

    // ④ 同行卡片高度差
    document.querySelectorAll('.sec-compare,.grid,.scene-grid,.release,.quality-grid,.g4').forEach(c => {
      if (getComputedStyle(c).display !== 'grid') return;
      const kids = Array.from(c.children).filter(k => rect(k).height > 0);
      if (kids.length < 2) return;
      // 按 top 分行
      const rows = {};
      kids.forEach(k => {
        const t = Math.round(rect(k).top);
        (rows[t] = rows[t] || []).push(k);
      });
      Object.values(rows).forEach(row => {
        if (row.length < 2) return;
        const hs = row.map(k => Math.round(rect(k).height));
        const d = Math.max(...hs) - Math.min(...hs);
        if (d > 4) {
          probs.push({ lv: 'P2', what: '同行卡片高度不等', d: `${c.className.split(' ')[0]} → ${hs.join(' / ')} 差 ${d}px` });
        }
      });
    });

    // ⑤ 网格列宽一致性
    document.querySelectorAll('.grid,.sec-compare,.scene-grid,.release,.quality-grid,.g4').forEach(c => {
      if (getComputedStyle(c).display !== 'grid') return;
      const kids = Array.from(c.children).filter(k => rect(k).width > 0);
      if (kids.length < 2) return;
      const ws = kids.map(k => Math.round(rect(k).width));
      const d = Math.max(...ws) - Math.min(...ws);
      if (d > 4) probs.push({ lv: 'P2', what: '网格列宽不等', d: `${c.className.split(' ')[0]} → ${ws.join(' / ')}` });
    });

    // ⑥ 卡片内元素左边缘（图标/h3/描述/标签应同左）
    document.querySelectorAll('.card').forEach(card => {
      const els = [card.querySelector('.icon-box'), card.querySelector('h3'), card.querySelector('.desc'), card.querySelector('.tagline')].filter(Boolean);
      if (els.length < 2) return;
      const ls = els.map(e => Math.round(rect(e).left));
      const d = Math.max(...ls) - Math.min(...ls);
      if (d > 2) {
        probs.push({ lv: 'P2', what: '卡片内左边缘不齐', d: `${card.querySelector('h3')?.textContent.slice(0,12)} → ${ls.join('/')} 差 ${d}px` });
      }
    });

    // ⑦ 场景卡 role 与 h3 间距（视觉粘连检查）
    document.querySelectorAll('.scene').forEach(sc => {
      const role = sc.querySelector('.role'), h3 = sc.querySelector('h3');
      if (!role || !h3) return;
      const gap = Math.round(rect(h3).top - rect(role).bottom);
      if (gap < 3) probs.push({ lv: 'P3', what: '场景卡 role 与标题间距过紧', d: `${h3.textContent.slice(0,10)} gap=${gap}px` });
    });

    return { pageMid, ctaMid, probs };
  });

  console.log(`页面中心=${r.pageMid}  收尾按钮组中心=${r.ctaMid}  偏差=${r.ctaMid - r.pageMid}px`);
  console.log('\n=== 对齐问题 ===');
  if (!r.probs.length) console.log('  ✅ 无');
  else {
    const g = {};
    r.probs.forEach(p => (g[p.lv] = g[p.lv] || []).push(p));
    ['P1', 'P2', 'P3'].forEach(lv => {
      if (!g[lv]) return;
      console.log(`  [${lv}] ${g[lv].length} 项`);
      g[lv].forEach(p => console.log(`    · ${p.what} — ${p.d}`));
    });
  }
  await browser.close();
})();
