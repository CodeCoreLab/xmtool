#!/usr/bin/env bash
# ============================================================
# Aireport 静态页样式守护检查（挂 cron 周期执行）
#
# 背景（2026-10-21）：
#   Aireport 拆分 tokens.css 后，根首页 /index.html 漏引该文件 →
#   全部 CSS 变量（--brand/--n-ink…）解析失败 → 整页白底。
#   而 style.css 仍返回 HTTP 200，用状态码查不出来。
#
# 判据（关键）：
#   只认真正的 <link rel="stylesheet" ...> 标签，
#   不能对着整份 HTML grep 关键字 —— 文件内的警示注释里也写着
#   "tokens.css"，会把「引用已被删除」误判为「仍然存在」（假阴性）。
#
# 本机出站被防火墙全拦截（qyapi 不通），故不在此推 webhook；
# 告警以 ALERT 行写入日志，由跳板机 relay 转发。
#
# 用法：bash /opt/workbuddy/scripts/check_static_style.sh
# 退出码：0 正常 / 1 发现问题
# ============================================================
set -uo pipefail

WEB="${AIREPORT_WEB:-/opt/aireport/web/aireport}"
LOG="${STYLE_GUARD_LOG:-/opt/workbuddy/logs/style_guard.log}"
FAIL=0
DETAIL=""

mkdir -p "$(dirname "$LOG")"

PAGES=$(grep -rl 'style\.css' "$WEB" --include='index.html' --exclude='*.bak*' 2>/dev/null | sort)

if [ -z "$PAGES" ]; then
  DETAIL="${DETAIL}未找到任何引用 style.css 的页面; "
  FAIL=1
else
  for page in $PAGES; do
    rel=${page#$WEB/}

    # 只取 <link ... stylesheet ...> 标签行，排除 HTML 注释里的说明文字
    # 做法：用 python 精确解析 link 标签（bash/grep 无法可靠区分注释与标签）
    RET=$(python3 - "$page" <<'PYEOF'
import re, sys
p = sys.argv[1]
s = open(p, encoding='utf-8', errors='replace').read()
# 先剥掉 HTML 注释，避免注释里的 "tokens.css" 被当成引用
s = re.sub(r'<!--.*?-->', '', s, flags=re.S)
# 再抓所有 stylesheet 链接
links = re.findall(r'<link[^>]+rel=["\']stylesheet["\'][^>]*>', s, flags=re.I)
hrefs = []
for l in links:
    m = re.search(r'href=["\']([^"\']+)["\']', l, flags=re.I)
    if m:
        hrefs.append(m.group(1))
tok_idx = next((i for i, h in enumerate(hrefs) if 'tokens.css' in h), -1)
sty_idx = next((i for i, h in enumerate(hrefs) if 'style.css' in h), -1)
has_style = sty_idx >= 0
if not has_style:
    print('SKIP')          # 该页不用 style.css（如 portal 自有一套），不在本检查范围
elif tok_idx < 0:
    print('MISSING')       # 用了 style.css 但没引 tokens.css
elif tok_idx > sty_idx:
    print('ORDER')         # tokens 在 style 之后
else:
    print('OK ' + hrefs[tok_idx] + ' ' + hrefs[sty_idx])
PYEOF
)
    case "$RET" in
      SKIP)
        ;;
      MISSING)
        DETAIL="${DETAIL}${rel} 缺 tokens.css 引用; "
        FAIL=1
        ;;
      ORDER)
        DETAIL="${DETAIL}${rel} tokens.css 排在 style.css 之后; "
        FAIL=1
        ;;
      OK*)
        ;;
      *)
        DETAIL="${DETAIL}${rel} 解析异常($RET); "
        FAIL=1
        ;;
    esac
  done
fi

# 关键变量抽查
TOK="$WEB/product/assets/tokens.css"
if [ ! -f "$TOK" ]; then
  DETAIL="${DETAIL}tokens.css 文件不存在; "
  FAIL=1
else
  for v in '--brand' '--n-ink' '--sev-p0'; do
    if ! grep -q -- "${v}:" "$TOK"; then
      DETAIL="${DETAIL}tokens 缺 ${v}; "
      FAIL=1
    fi
  done
fi

# 写日志（JSON 行 + 可读 ALERT 行）
TS=$(date -u +%Y-%m-%dT%H:%M:%SZ)
if [ "$FAIL" -eq 0 ]; then
  echo "{\"ts\":\"$TS\",\"status\":\"ok\"}" >> "$LOG"
else
  echo "{\"ts\":\"$TS\",\"status\":\"ALERT\",\"detail\":\"${DETAIL% }\"}" >> "$LOG"
  echo "ALERT $(date '+%F %T') $DETAIL" >> "$LOG"
fi

# 日志滚动
if [ "$(wc -l < "$LOG" 2>/dev/null || echo 0)" -gt 2000 ]; then
  tail -1000 "$LOG" > "$LOG.tmp" && mv "$LOG.tmp" "$LOG"
fi

# 手动执行时给可读输出
if [ -t 1 ]; then
  if [ "$FAIL" -eq 0 ]; then
    echo "[OK] 样式检查通过"
  else
    echo "[ALERT] $DETAIL"
  fi
fi

exit "$FAIL"
