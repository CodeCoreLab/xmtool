#!/usr/bin/env python3
"""把「小马桌面工具产品页」写入 deploy_report_worker 的真实队列目录。

worker 的 LOCAL_QUEUE_DIR = <SCRIPT_DIR>/.state/local_queue，
SCRIPT_DIR = ~/.workbuddy/scripts —— 必须写到这里，worker 才消费得到。
标题格式遵 parse_title：'<code>-<系统名> <YYYY-MM-DD>'，短横线前段即 web 树 code。
"""
import json
import os
import sys
from datetime import datetime

QUEUE_DIR = os.path.join(
    os.path.expanduser("~"), ".workbuddy", "scripts", ".state", "local_queue"
)
HTML_PATH = r"E:\xmtool\xmtool\docs\小马桌面工具-产品页.html"

TITLE = "XMTool-小马桌面工具产品页 2026-10-20"
PERIOD = "20261020"


def main() -> int:
    if not os.path.isfile(HTML_PATH):
        print(f"[FATAL] HTML 不存在: {HTML_PATH}")
        return 1
    with open(HTML_PATH, "r", encoding="utf-8") as f:
        html = f.read()

    payload = {
        "type": "other",
        "period": PERIOD,
        "title": TITLE,
        "tags": "产品页,远程桌面,小马远程桌面,WPF,.NET8,单文件发布",
        "author": "马欣",
        "department": "稳定性保障中心",
        "date_range_start": "2026-10-20",
        "date_range_end": "2026-10-20",
        "analyzed": 0,
        "md_content": "",
        "html_content": html,
    }

    os.makedirs(QUEUE_DIR, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d%H%M%S")
    fn = os.path.join(QUEUE_DIR, f"local_{stamp}_xmtool.json")
    with open(fn, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)

    print(f"[OK] 入队 {fn}")
    print(f"     title={TITLE} html={len(html)}B queue_dir={QUEUE_DIR}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
