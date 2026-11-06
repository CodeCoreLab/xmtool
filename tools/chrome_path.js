// A42：浏览器可执行文件路径解析（消除各脚本里硬编码的本机绝对路径）。
//
// 旧实现每个脚本都写死 `C:/Program Files/Google/Chrome/Application/chrome.exe`：
//   ① 换一台机器 / 换个浏览器安装位置就直接报 "browserType.launch: Executable
//      doesn't exist"；
//   ② 同一处路径在 15 个文件里重复，改一次要改 15 处。
//
// 解析顺序：
//   1. 环境变量 CHROME_PATH（显式指定，最高优先级）
//   2. 常见安装位置按序探测（Program Files / Program Files (x86) / LOCALAPPDATA）
//   3. 全部落空 → 返回 undefined，让 playwright 用自带的 chromium
const fs = require('fs');
const os = require('os');
const path = require('path');

function candidatePaths() {
  const list = [];
  if (process.env.CHROME_PATH) {
    list.push(process.env.CHROME_PATH);
  }
  const pf = process.env['ProgramFiles'] || 'C:\\Program Files';
  const pf86 = process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)';
  const local = process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local');
  list.push(path.join(pf, 'Google', 'Chrome', 'Application', 'chrome.exe'));
  list.push(path.join(pf86, 'Google', 'Chrome', 'Application', 'chrome.exe'));
  list.push(path.join(local, 'Google', 'Chrome', 'Application', 'chrome.exe'));
  return list;
}

/** 返回一个存在的 chrome 可执行文件路径；找不到则返回 undefined。 */
function resolveChromePath() {
  for (const p of candidatePaths()) {
    try {
      if (p && fs.existsSync(p)) return p;
    } catch (e) {
      // 路径不可访问（如权限问题）→ 试下一个
    }
  }
  return undefined;
}

/**
 * 生成给 playwright `chromium.launch({...})` 用的 options 片段。
 * 找到本机 Chrome 就带上 executablePath；否则不带（用 playwright 自带 chromium）。
 */
function chromeLaunchOptions(extra) {
  const opts = Object.assign({}, extra || {});
  const p = resolveChromePath();
  if (p) opts.executablePath = p;
  return opts;
}

module.exports = { resolveChromePath, chromeLaunchOptions, candidatePaths };
