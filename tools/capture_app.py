# -*- coding: utf-8 -*-
"""截取「小马远程桌面」程序真实界面截图，用于产品页展示。

做法：① 启动 exe ② 等待窗口就绪 ③ 用 Win32 PrintWindow 截取窗口位图
     ④ 保存为 PNG ⑤ 关闭程序。
说明：PrintWindow 可在无前台焦点时抓取窗口内容；对 WPF 窗口加
      PW_RENDERFULLCONTENT(2) 标志可正确抓取合成层。
"""
import ctypes
import ctypes.wintypes as wt
import os
import subprocess
import sys
import time

EXE = r'E:\xmtool\xmtool\releases\v1.0.0-20261020\小马远程桌面-v1.0.0-fdd-2.5MB.exe'
OUT_DIR = r'E:\xmtool\xmtool\docs\screenshots'
NAME = 'app-main.png'

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32

# ── 结构体 ──────────────────────────────────────────────────────────
class RECT(ctypes.Structure):
    _fields_ = [('left', wt.LONG), ('top', wt.LONG), ('right', wt.LONG), ('bottom', wt.LONG)]

class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [
        ('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG),
        ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD),
        ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG),
        ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD),
    ]

class BITMAPINFO(ctypes.Structure):
    _fields_ = [('bmiHeader', BITMAPINFOHEADER), ('bmiColors', wt.DWORD * 3)]


def find_window(pid, timeout=25):
    """按进程 PID 找主窗口句柄（可见 + 有标题）。"""
    found = []
    EnumProc = ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)

    def cb(hwnd, lparam):
        p = wt.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
        if p.value != pid:
            return True
        if not user32.IsWindowVisible(hwnd):
            return True
        n = user32.GetWindowTextLengthW(hwnd)
        if n == 0:
            return True
        found.append(hwnd)
        return True

    deadline = time.time() + timeout
    while time.time() < deadline:
        found.clear()
        user32.EnumWindows(EnumProc(cb), 0)
        if found:
            return found[0]
        time.sleep(0.5)
    return None


def capture(hwnd, path):
    """PrintWindow 截取窗口到 PNG。"""
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w = r.right - r.left
    h = r.bottom - r.top
    if w <= 0 or h <= 0:
        raise RuntimeError('窗口尺寸无效: %dx%d' % (w, h))

    hdc = user32.GetWindowDC(hwnd)
    mem = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mem, bmp)

    # PW_RENDERFULLCONTENT = 2，WPF 合成层必需
    ok = user32.PrintWindow(hwnd, mem, 2)
    if not ok:
        user32.PrintWindow(hwnd, mem, 0)   # 回退

    bi = BITMAPINFO()
    bi.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bi.bmiHeader.biWidth = w
    bi.bmiHeader.biHeight = -h          # 负值 = 自上而下，避免图像倒置
    bi.bmiHeader.biPlanes = 1
    bi.bmiHeader.biBitCount = 32
    bi.bmiHeader.biCompression = 0      # BI_RGB

    buf_len = w * h * 4
    buf = ctypes.create_string_buffer(buf_len)
    gdi32.GetDIBits(mem, bmp, 0, h, buf, ctypes.byref(bi), 0)

    # 写 PNG（纯标准库，避免依赖 PIL）
    _write_png(path, w, h, buf.raw)

    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mem)
    user32.ReleaseDC(hwnd, hdc)
    return w, h


def _write_png(path, w, h, bgra):
    """BGRA → PNG（标准库 zlib 手写，无第三方依赖）。"""
    import struct
    import zlib

    # BGRA → RGBA 且反转行序（GetDIBits 负高度已是自上而下）
    raw = bytearray()
    for y in range(h):
        row = bgra[y * w * 4:(y + 1) * w * 4]
        raw.append(0)                       # filter type 0
        for x in range(w):
            b, g, r, a = row[x * 4], row[x * 4 + 1], row[x * 4 + 2], row[x * 4 + 3]
            raw += bytes((r, g, b, a))

    def chunk(tag, data):
        c = struct.pack('>I', len(data)) + tag + data
        return c + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)

    ihdr = struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)   # 8bit RGBA
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr) \
        + chunk(b'IDAT', zlib.compress(bytes(raw), 6)) + chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(png)


def main():
    if not os.path.isfile(EXE):
        print('[FATAL] exe 不存在:', EXE)
        return 1
    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, NAME)

    # 清理可能残留的实例（程序单实例，旧进程会干扰）
    subprocess.run(['taskkill', '/F', '/IM', os.path.basename(EXE)],
                   capture_output=True)
    time.sleep(1)

    proc = subprocess.Popen([EXE], cwd=os.path.dirname(EXE))
    print('已启动 PID=%d，等待窗口…' % proc.pid)

    hwnd = find_window(proc.pid, timeout=25)
    if not hwnd:
        print('[FAIL] 未找到主窗口')
        proc.terminate()
        return 2

    # 等待界面渲染完成（WPF 首帧 + 数据加载）
    time.sleep(3.5)
    try:
        w, h = capture(hwnd, out)
        size = os.path.getsize(out)
        print('[OK] 截图 %dx%d → %s (%d B)' % (w, h, out, size))
    except Exception as e:
        print('[FAIL] 截图异常:', type(e).__name__, e)
        proc.terminate()
        return 3

    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
    print('程序已退出')
    return 0


if __name__ == '__main__':
    sys.exit(main())
