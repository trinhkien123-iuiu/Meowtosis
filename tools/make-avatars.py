"""Avatar mèo cùng theme Meowtosis, khung vuông bo góc đúng tỉ lệ avatar ở màn Home (khung 124, mèo 92x96: mèo đứng giữa, nguyên cả con).
Nhiều phong cách (kiểu thường + kiểu sự kiện), mỗi kiểu 12 màu mèo x 2 biểu cảm (mắt thường, mắt vui):
  Art/Avatars/<Kiểu>/Avatar_<Màu>.png, Avatar_<Màu>_Happy.png (256x256, nền trong suốt ngoài khung)
  Art/Avatars/<Kiểu>/Frame_<Kiểu>.png  khung rỗng ở giữa, để ghép lớp riêng trong game
  Art/Avatars/_Preview_<Kiểu>.png       bảng xem trước
  python tools/make-avatars.py [Kiểu ...]     (không tham số: làm tất cả)
Cần Python 3, numpy, Pillow. Hình mèo lấy từ Art/Shikacat/Cats (asset tạm)."""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art'
CATS = ROOT / 'Shikacat' / 'Cats'
OUT = ROOT / 'Avatars'

SIZE = 256
SS = 3
S = SIZE * SS
COLORS = ['Red', 'Orange', 'Yellow', 'Lime', 'Green', 'Teal', 'BabyBlue', 'ElectricBlue', 'Blue', 'Purple', 'Pink', 'WarmPink']

RADIUS = 0.23 * S           # bo góc ngoài
BORDER = 0.075 * S          # bề dày khung
CAT_HEIGHT = 0.70 * S       # mèo cao 70% khung, đứng giữa, chừa chỗ phía trên cho mũ / trang trí
CAT_BOTTOM = 0.07 * S       # cách đáy khung

# Mỗi kiểu: nền (trên, dưới), khung (sáng, giữa, tối), viền ngoài, trang trí
STYLES = {
    'Classic': dict(bg=((255, 236, 170), (255, 190, 110)), frame=((255, 226, 90), (255, 170, 20), (200, 100, 0)), edge=(150, 70, 0), deco='sparkle'),
    'Wood': dict(bg=((120, 36, 24), (70, 14, 10)), frame=((232, 160, 120), (196, 116, 84), (122, 64, 46)), edge=(92, 44, 30), deco='plank'),
    'Candy': dict(bg=((255, 214, 238), (190, 220, 255)), frame=((255, 170, 210), (255, 110, 170), (190, 50, 120)), edge=(150, 30, 90), deco='dots'),
    'Ocean': dict(bg=((140, 230, 255), (40, 120, 200)), frame=((140, 240, 255), (40, 170, 230), (10, 90, 160)), edge=(0, 60, 120), deco='bubbles'),
    'Night': dict(bg=((52, 40, 110), (18, 14, 52)), frame=((200, 190, 255), (130, 110, 220), (60, 44, 130)), edge=(30, 20, 80), deco='stars'),
    'Halloween': dict(bg=((96, 46, 140), (34, 14, 60)), frame=((255, 190, 80), (240, 120, 20), (150, 60, 0)), edge=(70, 28, 0), deco='halloween'),
    'Christmas': dict(bg=((18, 110, 70), (6, 56, 40)), frame=((255, 120, 110), (214, 40, 50), (130, 14, 28)), edge=(80, 8, 16), deco='christmas'),
    'Tet': dict(bg=((220, 40, 40), (140, 10, 20)), frame=((255, 236, 130), (240, 190, 50), (170, 110, 10)), edge=(110, 60, 0), deco='tet'),
    'Valentine': dict(bg=((255, 190, 214), (240, 110, 160)), frame=((255, 230, 240), (255, 130, 176), (190, 60, 110)), edge=(140, 30, 80), deco='valentine'),
    'Summer': dict(bg=((150, 230, 255), (255, 232, 150)), frame=((255, 250, 200), (255, 206, 70), (220, 130, 20)), edge=(150, 80, 0), deco='summer'),
    # --- Trang phục: mèo đội mũ, đeo kính... trên nền hợp chủ đề (khoá 'outfit' chọn bộ đồ, 'deco' chọn nền) ---
    'Beach': dict(bg=((120, 214, 255), (255, 228, 150)), frame=((255, 246, 190), (255, 190, 60), (210, 120, 20)), edge=(140, 80, 0), deco='beach', outfit='beach'),
    'Desert': dict(bg=((255, 196, 110), (214, 110, 50)), frame=((255, 214, 140), (214, 140, 60), (140, 78, 28)), edge=(96, 50, 16), deco='desert', outfit='desert'),
    'Pirate': dict(bg=((50, 120, 150), (14, 40, 70)), frame=((230, 200, 130), (170, 120, 60), (90, 56, 28)), edge=(50, 30, 12), deco='pirate', outfit='pirate'),
    'Chef': dict(bg=((255, 244, 220), (240, 200, 160)), frame=((255, 255, 255), (214, 214, 224), (140, 140, 160)), edge=(90, 90, 110), deco='kitchen', outfit='chef'),
    'Wizard': dict(bg=((90, 60, 170), (24, 14, 70)), frame=((255, 230, 120), (220, 170, 50), (130, 90, 20)), edge=(70, 46, 10), deco='stars', outfit='wizard'),
    'Astronaut': dict(bg=((40, 30, 90), (6, 6, 30)), frame=((240, 244, 255), (170, 180, 210), (90, 100, 140)), edge=(40, 46, 80), deco='cosmos', outfit='astronaut'),
    'Royal': dict(bg=((150, 60, 170), (70, 20, 100)), frame=((255, 240, 140), (240, 190, 40), (160, 100, 10)), edge=(100, 56, 0), deco='royal', outfit='royal'),
    'Cowboy': dict(bg=((255, 170, 90), (200, 80, 50)), frame=((220, 160, 100), (160, 100, 50), (90, 52, 24)), edge=(56, 30, 10), deco='desert', outfit='cowboy'),
    'Ninja': dict(bg=((60, 110, 80), (14, 40, 34)), frame=((120, 130, 140), (70, 78, 90), (30, 36, 46)), edge=(10, 14, 20), deco='bamboo', outfit='ninja'),
    'Party': dict(bg=((255, 214, 240), (190, 200, 255)), frame=((255, 180, 220), (255, 120, 180), (190, 60, 120)), edge=(140, 30, 90), deco='confetti', outfit='party'),
    'Sailor': dict(bg=((150, 210, 255), (60, 130, 210)), frame=((255, 255, 255), (80, 130, 210), (24, 60, 130)), edge=(14, 36, 90), deco='bubbles', outfit='sailor'),
    'Winter': dict(bg=((190, 224, 255), (120, 160, 220)), frame=((255, 255, 255), (150, 190, 240), (80, 110, 180)), edge=(40, 70, 130), deco='snowy', outfit='winter'),
}


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def vertical_gradient(top, bottom):
    t = np.linspace(0, 1, S, dtype=np.float32)[:, None, None]
    arr = np.array(top, np.float32) * (1 - t) + np.array(bottom, np.float32) * t
    arr = np.broadcast_to(arr, (S, S, 3))
    return Image.fromarray(np.dstack([arr, np.full((S, S), 255, np.float32)]).astype(np.uint8), 'RGBA')


def rounded_mask(inset, radius):
    m = Image.new('L', (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle((inset, inset, S - 1 - inset, S - 1 - inset), radius=radius, fill=255)
    return m


def star4(d, cx, cy, r, fill):
    pts = []
    for k in range(8):
        ang = k * math.pi / 4 - math.pi / 2
        rad = r if k % 2 == 0 else r * 0.28
        pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
    d.polygon(pts, fill=fill)


def heart(d, cx, cy, size, fill):
    r = size * 0.27
    d.ellipse((cx - size * 0.5, cy - size * 0.42, cx - size * 0.5 + 2 * r * 1.1, cy - size * 0.42 + 2 * r * 1.1), fill=fill)
    d.ellipse((cx + size * 0.5 - 2 * r * 1.1, cy - size * 0.42, cx + size * 0.5, cy - size * 0.42 + 2 * r * 1.1), fill=fill)
    d.polygon([(cx - size * 0.49, cy - size * 0.1), (cx + size * 0.49, cy - size * 0.1), (cx, cy + size * 0.5)], fill=fill)


def flower(d, cx, cy, r, petal, center):
    for k in range(5):
        ang = k * 2 * math.pi / 5 - math.pi / 2
        px, py = cx + math.cos(ang) * r * 0.62, cy + math.sin(ang) * r * 0.62
        d.ellipse((px - r * 0.45, py - r * 0.45, px + r * 0.45, py + r * 0.45), fill=petal)
    d.ellipse((cx - r * 0.28, cy - r * 0.28, cx + r * 0.28, cy + r * 0.28), fill=center)


def bat(d, cx, cy, w, fill):
    d.ellipse((cx - w * 0.1, cy - w * 0.12, cx + w * 0.1, cy + w * 0.16), fill=fill)
    for sgn in (-1, 1):
        d.polygon([(cx + sgn * w * 0.06, cy - w * 0.04), (cx + sgn * w * 0.5, cy - w * 0.22), (cx + sgn * w * 0.42, cy + w * 0.02),
                   (cx + sgn * w * 0.3, cy + w * 0.1), (cx + sgn * w * 0.2, cy + w * 0.04), (cx + sgn * w * 0.1, cy + w * 0.14)], fill=fill)


def pumpkin(d, cx, cy, w):
    d.ellipse((cx - w * 0.5, cy - w * 0.36, cx + w * 0.5, cy + w * 0.42), fill=(245, 130, 20))
    d.ellipse((cx - w * 0.28, cy - w * 0.36, cx + w * 0.28, cy + w * 0.42), fill=(255, 160, 40))
    d.rectangle((cx - w * 0.05, cy - w * 0.52, cx + w * 0.07, cy - w * 0.34), fill=(70, 120, 30))
    d.polygon([(cx - w * 0.22, cy - w * 0.06), (cx - w * 0.1, cy - w * 0.06), (cx - w * 0.16, cy + w * 0.06)], fill=(60, 20, 0))
    d.polygon([(cx + w * 0.22, cy - w * 0.06), (cx + w * 0.1, cy - w * 0.06), (cx + w * 0.16, cy + w * 0.06)], fill=(60, 20, 0))
    d.polygon([(cx - w * 0.2, cy + w * 0.12), (cx + w * 0.2, cy + w * 0.12), (cx + w * 0.12, cy + w * 0.2), (cx - w * 0.12, cy + w * 0.2)], fill=(60, 20, 0))


def snowflake(d, cx, cy, r, fill):
    for k in range(3):
        ang = k * math.pi / 3
        dx, dy = math.cos(ang) * r, math.sin(ang) * r
        d.line((cx - dx, cy - dy, cx + dx, cy + dy), fill=fill, width=max(2, int(r * 0.16)))


def decorate_back(style, rng):
    """Trang trí phía sau mèo."""
    layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    deco = style['deco']
    if deco == 'sparkle':
        for _ in range(7):
            star4(d, rng.uniform(0.12, 0.88) * S, rng.uniform(0.12, 0.6) * S, rng.uniform(0.03, 0.06) * S, (255, 255, 255, 200))
    elif deco == 'plank':
        for k in (0.34, 0.67):
            d.line((0, k * S, S, k * S), fill=(40, 8, 4, 90), width=SS * 2)
    elif deco == 'dots':
        for _ in range(14):
            r = rng.uniform(0.012, 0.03) * S
            x, y = rng.uniform(0.1, 0.9) * S, rng.uniform(0.1, 0.9) * S
            d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, 170))
    elif deco == 'bubbles':
        for _ in range(9):
            r = rng.uniform(0.02, 0.05) * S
            x, y = rng.uniform(0.1, 0.9) * S, rng.uniform(0.1, 0.85) * S
            d.ellipse((x - r, y - r, x + r, y + r), outline=(255, 255, 255, 190), width=SS * 2)
            d.ellipse((x - r * 0.4, y - r * 0.5, x - r * 0.1, y - r * 0.2), fill=(255, 255, 255, 220))
    elif deco == 'stars':
        for _ in range(16):
            r = rng.uniform(0.008, 0.03) * S
            x, y = rng.uniform(0.08, 0.92) * S, rng.uniform(0.08, 0.7) * S
            star4(d, x, y, r * 1.8, (255, 246, 200, 230))
        d.ellipse((0.66 * S, 0.1 * S, 0.84 * S, 0.28 * S), fill=(255, 244, 190, 255))
        d.ellipse((0.72 * S, 0.08 * S, 0.9 * S, 0.26 * S), fill=(40, 30, 100, 255))
    elif deco == 'halloween':
        d.ellipse((0.58 * S, 0.1 * S, 0.84 * S, 0.36 * S), fill=(255, 236, 160, 255))
        bat(d, 0.2 * S, 0.22 * S, 0.2 * S, (20, 8, 40, 255))
        bat(d, 0.42 * S, 0.12 * S, 0.12 * S, (20, 8, 40, 255))
        pumpkin(d, 0.16 * S, 0.8 * S, 0.2 * S)
    elif deco == 'christmas':
        for _ in range(16):
            r = rng.uniform(0.01, 0.025) * S
            x, y = rng.uniform(0.08, 0.92) * S, rng.uniform(0.08, 0.9) * S
            d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, 220))
        snowflake(d, 0.17 * S, 0.2 * S, 0.07 * S, (255, 255, 255, 230))
        snowflake(d, 0.84 * S, 0.3 * S, 0.055 * S, (255, 255, 255, 230))
    elif deco == 'tet':
        for (x, y, r) in ((0.15, 0.16, 0.075), (0.86, 0.22, 0.06), (0.12, 0.7, 0.05), (0.88, 0.76, 0.07)):
            flower(d, x * S, y * S, r * S, (255, 214, 60, 255), (200, 40, 40, 255))
        for _ in range(5):
            r = rng.uniform(0.02, 0.035) * S
            x, y = rng.uniform(0.1, 0.9) * S, rng.uniform(0.1, 0.5) * S
            d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 220, 90, 230))
    elif deco == 'valentine':
        for _ in range(8):
            heart(d, rng.uniform(0.1, 0.9) * S, rng.uniform(0.1, 0.8) * S, rng.uniform(0.05, 0.11) * S, (255, 255, 255, 190))
        heart(d, 0.84 * S, 0.2 * S, 0.13 * S, (230, 40, 90, 255))
    elif deco == 'summer':
        d.ellipse((0.7 * S, 0.06 * S, 0.92 * S, 0.28 * S), fill=(255, 220, 60, 255))
        for k in range(10):
            ang = k * math.pi / 5
            d.line((0.81 * S + math.cos(ang) * 0.13 * S, 0.17 * S + math.sin(ang) * 0.13 * S,
                    0.81 * S + math.cos(ang) * 0.17 * S, 0.17 * S + math.sin(ang) * 0.17 * S), fill=(255, 220, 60, 255), width=SS * 3)
        d.pieslice((-0.2 * S, 0.84 * S, 0.5 * S, 1.3 * S), 180, 360, fill=(255, 220, 140, 255))
    elif deco == 'beach':
        d.ellipse((0.66 * S, 0.08 * S, 0.9 * S, 0.32 * S), fill=(255, 226, 80, 255))
        d.rectangle((0, 0.74 * S, S, S), fill=(250, 220, 150, 255))
        for k in range(3):
            y = (0.66 + 0.03 * k) * S
            d.arc((-0.1 * S, y, 0.5 * S, y + 0.1 * S), 180, 360, fill=(255, 255, 255, 220), width=SS * 3)
            d.arc((0.4 * S, y, 1.0 * S, y + 0.1 * S), 180, 360, fill=(255, 255, 255, 220), width=SS * 3)
        for ang in (-60, -20, 20):
            d.line((0.12 * S, 0.5 * S, 0.12 * S + math.cos(math.radians(ang)) * 0.16 * S, 0.5 * S + math.sin(math.radians(ang)) * 0.16 * S), fill=(40, 150, 70, 255), width=SS * 6)
        d.line((0.12 * S, 0.5 * S, 0.1 * S, 0.78 * S), fill=(130, 84, 40, 255), width=SS * 5)
    elif deco == 'desert':
        d.ellipse((0.16 * S, 0.14 * S, 0.4 * S, 0.38 * S), fill=(255, 236, 150, 255))
        d.polygon([(0, 0.78 * S), (0.3 * S, 0.6 * S), (0.62 * S, 0.8 * S), (S, 0.62 * S), (S, S), (0, S)], fill=(230, 150, 80, 255))
        d.polygon([(0, 0.9 * S), (0.4 * S, 0.74 * S), (S, 0.92 * S), (S, S), (0, S)], fill=(200, 120, 60, 255))
        d.rounded_rectangle((0.8 * S, 0.5 * S, 0.86 * S, 0.8 * S), radius=SS * 6, fill=(60, 150, 70, 255))
        d.rounded_rectangle((0.74 * S, 0.58 * S, 0.8 * S, 0.68 * S), radius=SS * 5, fill=(60, 150, 70, 255))
        d.rounded_rectangle((0.86 * S, 0.54 * S, 0.92 * S, 0.64 * S), radius=SS * 5, fill=(60, 150, 70, 255))
    elif deco == 'pirate':
        d.ellipse((0.7 * S, 0.1 * S, 0.88 * S, 0.28 * S), fill=(255, 244, 200, 255))
        for k in range(4):
            y = (0.64 + 0.07 * k) * S
            d.arc((-0.1 * S, y, 0.45 * S, y + 0.12 * S), 180, 360, fill=(120, 200, 220, 200), width=SS * 3)
            d.arc((0.35 * S, y, 0.95 * S, y + 0.12 * S), 180, 360, fill=(120, 200, 220, 200), width=SS * 3)
        d.polygon([(0.1 * S, 0.5 * S), (0.1 * S, 0.3 * S), (0.24 * S, 0.5 * S)], fill=(240, 240, 230, 255))
        d.line((0.1 * S, 0.5 * S, 0.1 * S, 0.62 * S), fill=(90, 56, 28, 255), width=SS * 4)
    elif deco == 'kitchen':
        step = S / 6
        for r in range(6):
            for c in range(6):
                if (r + c) % 2 == 0:
                    d.rectangle((c * step, r * step, (c + 1) * step, (r + 1) * step), fill=(255, 255, 255, 90))
    elif deco == 'cosmos':
        for _ in range(22):
            r = rng.uniform(0.006, 0.02) * S
            x, y = rng.uniform(0.06, 0.94) * S, rng.uniform(0.06, 0.94) * S
            d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, 230))
        d.ellipse((0.66 * S, 0.1 * S, 0.9 * S, 0.34 * S), fill=(255, 150, 80, 255))
        d.ellipse((0.6 * S, 0.19 * S, 0.96 * S, 0.25 * S), outline=(255, 230, 160, 255), width=SS * 3)
        d.ellipse((0.08 * S, 0.7 * S, 0.2 * S, 0.82 * S), fill=(120, 200, 255, 255))
    elif deco == 'royal':
        for _ in range(10):
            star4(d, rng.uniform(0.1, 0.9) * S, rng.uniform(0.1, 0.7) * S, rng.uniform(0.02, 0.045) * S, (255, 230, 120, 230))
        d.rectangle((0, 0.84 * S, S, S), fill=(190, 30, 60, 255))
    elif deco == 'confetti':
        palette = [(255, 80, 120), (255, 210, 60), (80, 200, 255), (120, 220, 120), (190, 120, 255)]
        for _ in range(26):
            x, y = rng.uniform(0.08, 0.92) * S, rng.uniform(0.08, 0.92) * S
            w = rng.uniform(0.02, 0.04) * S
            d.polygon([(x, y), (x + w, y + w * 0.4), (x + w * 0.7, y + w), (x - w * 0.2, y + w * 0.6)], fill=rng.choice(palette) + (255,))
    elif deco == 'bamboo':
        for x in (0.1, 0.24, 0.8, 0.92):
            d.rectangle((x * S, 0, (x + 0.05) * S, S), fill=(110, 190, 110, 200))
            for y in (0.2, 0.5, 0.8):
                d.rectangle((x * S - SS, y * S, (x + 0.05) * S + SS, y * S + SS * 3), fill=(60, 130, 70, 255))
        d.ellipse((0.56 * S, 0.1 * S, 0.76 * S, 0.3 * S), fill=(240, 250, 200, 255))
    elif deco == 'snowy':
        for _ in range(20):
            r = rng.uniform(0.01, 0.026) * S
            x, y = rng.uniform(0.08, 0.92) * S, rng.uniform(0.08, 0.8) * S
            d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, 235))
        d.ellipse((-0.2 * S, 0.8 * S, 1.2 * S, 1.3 * S), fill=(255, 255, 255, 255))
        snowflake(d, 0.17 * S, 0.2 * S, 0.07 * S, (255, 255, 255, 240))
    return layer


def decorate_front(style, cat_box):
    """Trang trí đè lên mèo: mũ, nơ... cat_box = (x0, y0, x1, y1) của con mèo trong khung."""
    deco = style['deco']
    layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = cat_box
    w, h = x1 - x0, y1 - y0
    cx = (x0 + x1) / 2
    if deco == 'christmas':
        # mũ ông già Noel đội lệch trên đầu
        band_y = y0 + h * 0.13
        left, right = cx - w * 0.27, cx + w * 0.27
        d.polygon([(left, band_y), (right, band_y), (cx + w * 0.34, y0 - h * 0.12), (cx + w * 0.46, y0 + h * 0.02), (cx + w * 0.1, y0 - h * 0.2)], fill=(214, 40, 50, 255))
        d.polygon([(left, band_y), (right, band_y), (cx + w * 0.1, y0 - h * 0.2), (cx - w * 0.15, y0 - h * 0.05)], fill=(214, 40, 50, 255))
        d.rounded_rectangle((left - w * 0.03, band_y - h * 0.05, right + w * 0.03, band_y + h * 0.04), radius=h * 0.04, fill=(255, 255, 255, 255))
        d.ellipse((cx + w * 0.38, y0 - h * 0.1, cx + w * 0.54, y0 + h * 0.06), fill=(255, 255, 255, 255))
    elif deco == 'halloween':
        # mũ phù thuỷ
        band_y = y0 + h * 0.12
        d.polygon([(cx - w * 0.28, band_y), (cx + w * 0.28, band_y), (cx + w * 0.05, y0 - h * 0.25)], fill=(60, 30, 100, 255))
        d.polygon([(cx - w * 0.38, band_y), (cx + w * 0.38, band_y), (cx + w * 0.3, band_y + h * 0.04), (cx - w * 0.3, band_y + h * 0.04)], fill=(40, 20, 80, 255))
        d.rectangle((cx - w * 0.27, band_y - h * 0.07, cx + w * 0.27, band_y), fill=(245, 130, 20, 255))
    elif deco == 'tet':
        # bao lì xì cầm ở góc dưới
        bw, bh = w * 0.3, h * 0.34
        bx, by = x1 - bw * 0.7, y1 - bh * 0.95
        d.rounded_rectangle((bx, by, bx + bw, by + bh), radius=bw * 0.12, fill=(230, 30, 40, 255), outline=(255, 214, 60, 255), width=SS * 2)
        d.ellipse((bx + bw * 0.3, by + bh * 0.3, bx + bw * 0.7, by + bh * 0.62), outline=(255, 214, 60, 255), width=SS * 2)
    elif deco == 'valentine':
        heart(d, x1 - w * 0.08, y1 - h * 0.22, w * 0.28, (230, 40, 90, 255))
    elif deco == 'summer':
        # kính râm không hợp từng biểu cảm nên chỉ thêm vòng hoa nhỏ trên tai
        flower(d, x0 + w * 0.2, y0 + h * 0.1, w * 0.1, (255, 130, 170, 255), (255, 230, 90, 255))
    return layer


def shape(d, pts, fill, edge, lw=SS * 2):
    d.polygon(pts, fill=fill)
    d.line(list(pts) + [pts[0]], fill=edge, width=lw, joint='curve')


def oval(d, cx, cy, rx, ry, fill, edge=None, lw=SS * 2):
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=fill, outline=edge, width=lw if edge else 0)


def sunglasses(d, P, w, h, frame=(30, 30, 44, 255)):
    for fx in (0.285, 0.625):
        cx, cy = P(fx, 0.29)
        d.rounded_rectangle((cx - 0.135 * w, cy - 0.085 * h, cx + 0.135 * w, cy + 0.085 * h), radius=0.05 * h, fill=frame)
        d.line((cx - 0.09 * w, cy - 0.04 * h, cx - 0.02 * w, cy - 0.04 * h), fill=(255, 255, 255, 120), width=SS * 3)
    a, b = P(0.285, 0.29), P(0.625, 0.29)
    d.line((a[0] + 0.13 * w, a[1], b[0] - 0.13 * w, b[1]), fill=frame, width=SS * 4)
    d.line((a[0] - 0.13 * w, a[1], P(0.05, 0.27)[0], P(0.05, 0.27)[1]), fill=frame, width=SS * 4)
    d.line((b[0] + 0.13 * w, b[1], P(0.86, 0.27)[0], P(0.86, 0.27)[1]), fill=frame, width=SS * 4)


def straw_hat(d, P, w, h):
    oval(d, *P(0.455, 0.06), 0.28 * w, 0.14 * h, (244, 206, 120, 255), (160, 110, 40, 255))
    d.rectangle((P(0.2, 0.07)[0], P(0.2, 0.07)[1], P(0.71, 0.115)[0], P(0.71, 0.115)[1]), fill=(226, 60, 60, 255))
    oval(d, *P(0.455, 0.125), 0.46 * w, 0.065 * h, (248, 214, 132, 255), (160, 110, 40, 255))


def outfit_front(style, cat_box):
    """Trang phục vẽ đè lên mèo. Toạ độ theo tỉ lệ hộp bao của mèo: mặt nằm giữa x=0,455, mắt y=0,29, trán y=0,12."""
    name = style['outfit']
    x0, y0, x1, y1 = cat_box
    w, h = x1 - x0, y1 - y0
    P = lambda fx, fy: (x0 + fx * w, y0 + fy * h)
    layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    edge = (60, 30, 20, 255)
    if name == 'beach':
        cx, cy = P(0.455, 0.8)
        rx, ry = 0.38 * w, 0.11 * h
        for k in range(8):
            d.arc((cx - rx, cy - ry, cx + rx, cy + ry), k * 45, k * 45 + 45, fill=(255, 255, 255, 255) if k % 2 else (230, 60, 60, 255), width=int(0.07 * h))
        straw_hat(d, P, w, h)
        sunglasses(d, P, w, h)
    elif name == 'desert':
        oval(d, *P(0.455, 0.06), 0.34 * w, 0.13 * h, (246, 232, 200, 255), (150, 110, 60, 255))
        for k in range(3):
            d.arc((P(0.455, 0)[0] - (0.3 - k * 0.05) * w, P(0, 0.0)[1], P(0.455, 0)[0] + (0.3 - k * 0.05) * w, P(0, 0.12)[1] + k * 0.01 * h), 200, 340, fill=(200, 160, 100, 255), width=SS * 2)
        d.rectangle((P(0.14, 0.09)[0], P(0, 0.09)[1], P(0.77, 0.14)[0], P(0, 0.14)[1]), fill=(190, 50, 60, 255))
        oval(d, *P(0.455, 0.115), 0.04 * w, 0.04 * h, (80, 200, 220, 255), edge)
        sunglasses(d, P, w, h, (90, 56, 30, 255))
    elif name == 'cowboy':
        shape(d, [P(0.3, 0.13), P(0.33, -0.1), P(0.4, -0.06), P(0.455, -0.1), P(0.52, -0.06), P(0.58, -0.1), P(0.61, 0.13)], (150, 92, 44, 255), (80, 44, 16, 255))
        d.rectangle((P(0.3, 0.06)[0], P(0, 0.06)[1], P(0.61, 0.12)[0], P(0, 0.12)[1]), fill=(90, 50, 24, 255))
        oval(d, *P(0.455, 0.12), 0.46 * w, 0.06 * h, (164, 104, 52, 255), (80, 44, 16, 255))
        shape(d, [P(0.2, 0.58), P(0.71, 0.58), P(0.455, 0.78)], (214, 50, 50, 255), (110, 20, 20, 255))
    elif name == 'pirate':
        shape(d, [P(0.04, 0.14), P(0.2, 0.0), P(0.455, -0.14), P(0.71, 0.0), P(0.87, 0.14), P(0.7, 0.18), P(0.455, 0.09), P(0.21, 0.18)], (36, 36, 48, 255), (230, 190, 90, 255))
        oval(d, *P(0.455, 0.0), 0.05 * w, 0.045 * h, (255, 255, 255, 255), edge)
        a = P(0.285, 0.29)
        d.line((a[0] - 0.2 * w, a[1] - 0.12 * h, a[0] + 0.5 * w, a[1] + 0.1 * h), fill=(30, 30, 40, 255), width=SS * 3)
        oval(d, a[0], a[1], 0.095 * w, 0.075 * h, (30, 30, 40, 255))
    elif name == 'chef':
        for fx, fy, r in ((0.29, -0.04, 0.15), (0.455, -0.12, 0.16), (0.62, -0.04, 0.15)):
            oval(d, *P(fx, fy), r * w, r * w, (255, 255, 255, 255), (170, 170, 190, 255))
        d.rounded_rectangle((P(0.2, 0.0)[0], P(0, 0.0)[1], P(0.71, 0.0)[0], P(0, 0.13)[1]), radius=SS * 6, fill=(255, 255, 255, 255), outline=(170, 170, 190, 255), width=SS * 2)
        d.rectangle((P(0.2, 0.095)[0], P(0, 0.095)[1], P(0.71, 0.13)[0], P(0, 0.13)[1]), fill=(225, 225, 240, 255))
        shape(d, [P(0.22, 0.6), P(0.455, 0.74), P(0.69, 0.6), P(0.62, 0.8), P(0.29, 0.8)], (255, 255, 255, 255), (170, 170, 190, 255))
    elif name == 'wizard':
        shape(d, [P(0.2, 0.13), P(0.7, 0.13), P(0.53, -0.36), P(0.44, -0.3)], (50, 56, 150, 255), (20, 20, 80, 255))
        oval(d, *P(0.455, 0.125), 0.44 * w, 0.06 * h, (60, 66, 170, 255), (20, 20, 80, 255))
        d.rectangle((P(0.2, 0.08)[0], P(0, 0.08)[1], P(0.7, 0.125)[0], P(0, 0.125)[1]), fill=(240, 200, 60, 255))
        for fx, fy, r in ((0.45, -0.05, 0.05), (0.54, -0.2, 0.035), (0.38, 0.04, 0.03)):
            star4(d, *P(fx, fy), r * w * 1.6, (255, 230, 100, 255))
    elif name == 'astronaut':
        c = P(0.455, 0.3)
        r = 0.55 * w
        d.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=(180, 225, 255, 55), outline=(245, 248, 255, 255), width=SS * 6)
        d.arc((c[0] - r * 0.8, c[1] - r * 0.8, c[0] + r * 0.8, c[1] + r * 0.8), 200, 250, fill=(255, 255, 255, 230), width=SS * 5)
    elif name == 'royal':
        shape(d, [P(0.26, 0.13), P(0.24, -0.06), P(0.34, 0.03), P(0.455, -0.1), P(0.575, 0.03), P(0.67, -0.06), P(0.65, 0.13)], (255, 206, 50, 255), (140, 90, 10, 255))
        d.rectangle((P(0.26, 0.1)[0], P(0, 0.1)[1], P(0.65, 0.13)[0], P(0, 0.13)[1]), fill=(230, 170, 30, 255))
        for fx, col in ((0.34, (230, 40, 70, 255)), (0.455, (60, 120, 240, 255)), (0.575, (230, 40, 70, 255))):
            oval(d, *P(fx, 0.085), 0.025 * w, 0.025 * w, col, (120, 70, 0, 255), SS)
        shape(d, [P(0.12, 0.6), P(0.455, 0.7), P(0.8, 0.6), P(0.76, 0.84), P(0.455, 0.8), P(0.16, 0.84)], (190, 30, 60, 255), (100, 10, 30, 255))
        for fx in (0.3, 0.455, 0.61):
            oval(d, *P(fx, 0.72), 0.02 * w, 0.02 * w, (255, 250, 240, 255))
    elif name == 'ninja':
        d.rounded_rectangle((P(0.04, 0.08)[0], P(0, 0.08)[1], P(0.86, 0.17)[0], P(0, 0.17)[1]), radius=SS * 6, fill=(30, 40, 90, 255), outline=(10, 14, 40, 255), width=SS * 2)
        d.rounded_rectangle((P(0.34, 0.075)[0], P(0, 0.075)[1], P(0.57, 0.175)[0], P(0, 0.175)[1]), radius=SS * 4, fill=(200, 208, 220, 255), outline=(90, 96, 110, 255), width=SS * 2)
        star4(d, *P(0.455, 0.125), 0.045 * w, (90, 96, 110, 255))
        shape(d, [P(0.84, 0.1), P(1.0, 0.0), P(0.98, 0.1), P(1.04, 0.2), P(0.88, 0.16)], (30, 40, 90, 255), (10, 14, 40, 255))
    elif name == 'party':
        shape(d, [P(0.24, 0.14), P(0.67, 0.14), P(0.455, -0.36)], (255, 110, 170, 255), (160, 40, 100, 255))
        for fx, fy in ((0.4, 0.04), (0.52, 0.07), (0.455, -0.1), (0.38, 0.11), (0.5, -0.2)):
            oval(d, *P(fx, fy), 0.02 * w, 0.02 * w, (255, 230, 90, 255))
        d.arc((P(0.24, 0)[0], P(0, 0.08)[1], P(0.67, 0)[0], P(0, 0.2)[1]), 0, 180, fill=(80, 200, 255, 255), width=SS * 4)
        oval(d, *P(0.455, -0.37), 0.05 * w, 0.05 * w, (255, 230, 90, 255), (160, 120, 10, 255))
    elif name == 'sailor':
        oval(d, *P(0.455, 0.07), 0.3 * w, 0.1 * h, (255, 255, 255, 255), (150, 160, 190, 255))
        d.rectangle((P(0.155, 0.09)[0], P(0, 0.09)[1], P(0.76, 0.14)[0], P(0, 0.14)[1]), fill=(30, 60, 130, 255))
        oval(d, *P(0.455, 0.14), 0.3 * w, 0.03 * h, (30, 60, 130, 255))
        shape(d, [P(0.14, 0.58), P(0.455, 0.8), P(0.77, 0.58), P(0.72, 0.54), P(0.455, 0.68), P(0.2, 0.54)], (30, 60, 130, 255), (255, 255, 255, 255))
        oval(d, *P(0.455, 0.7), 0.03 * w, 0.03 * w, (230, 60, 60, 255))
    elif name == 'winter':
        oval(d, *P(0.455, 0.07), 0.3 * w, 0.12 * h, (214, 50, 60, 255), (110, 20, 30, 255))
        d.rounded_rectangle((P(0.15, 0.085)[0], P(0, 0.085)[1], P(0.76, 0.155)[0], P(0, 0.155)[1]), radius=SS * 8, fill=(240, 90, 100, 255), outline=(110, 20, 30, 255), width=SS * 2)
        for k in range(10):
            x = P(0.18 + k * 0.058, 0)[0]
            d.line((x, P(0, 0.095)[1], x, P(0, 0.15)[1]), fill=(180, 40, 50, 255), width=SS * 2)
        oval(d, *P(0.455, -0.08), 0.07 * w, 0.07 * w, (255, 255, 255, 255), (170, 180, 200, 255))
        d.rounded_rectangle((P(0.1, 0.58)[0], P(0, 0.58)[1], P(0.82, 0.68)[0], P(0, 0.68)[1]), radius=SS * 10, fill=(255, 255, 255, 255), outline=(150, 160, 190, 255), width=SS * 2)
        for k in range(6):
            x = P(0.15 + k * 0.12, 0)[0]
            d.line((x, P(0, 0.585)[1], x + 0.02 * w, P(0, 0.675)[1]), fill=(214, 50, 60, 255), width=SS * 5)
        d.rounded_rectangle((P(0.62, 0.64)[0], P(0, 0.64)[1], P(0.74, 0.86)[0], P(0, 0.86)[1]), radius=SS * 6, fill=(255, 255, 255, 255), outline=(150, 160, 190, 255), width=SS * 2)
    return layer


def render(style_name, style, color, happy):
    name = f'Image_Cat_{color}' + ('EndGame' if happy else '') + '.png'
    cat = Image.open(CATS / name).convert('RGBA')
    scale = CAT_HEIGHT / cat.height
    cat = cat.resize((int(cat.width * scale), int(CAT_HEIGHT)), Image.LANCZOS)
    cx0 = int((S - cat.width) / 2 + 0.025 * S)    # thân mèo nằm lệch trái hộp bao (đuôi thò bên phải): dịch nhẹ để thân nằm giữa khung
    cy0 = int(S - CAT_BOTTOM - cat.height)
    cat_box = (cx0, cy0, cx0 + cat.width, cy0 + cat.height)

    outer = rounded_mask(0, RADIUS)
    inner_radius = max(RADIUS - BORDER * 0.8, BORDER)
    inner = rounded_mask(BORDER, inner_radius)

    rng = random.Random(hash(style_name) % 1000)       # trang trí cố định theo kiểu
    background = vertical_gradient(*style['bg'])
    back = decorate_back(style, rng)
    front = outfit_front(style, cat_box) if 'outfit' in style else decorate_front(style, cat_box)

    shadow = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    mask_a = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    mask_a.paste(cat, (cx0, cy0), cat)
    soft = mask_a.split()[3].filter(ImageFilter.GaussianBlur(SS * 4)).point(lambda v: int(v * 0.3))
    shadow.paste((0, 0, 0, 255), (0, SS * 5), soft)

    scene = Image.alpha_composite(background, back)
    scene = Image.alpha_composite(scene, shadow)
    scene = Image.alpha_composite(scene, mask_a)
    scene = Image.alpha_composite(scene, front)
    inner_glow = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(inner_glow).rounded_rectangle((BORDER, BORDER, S - 1 - BORDER, S - 1 - BORDER), radius=inner_radius, outline=(0, 0, 0, 120), width=SS * 4)
    scene = Image.alpha_composite(scene, inner_glow.filter(ImageFilter.GaussianBlur(SS * 2)))

    frame = frame_image(style)
    canvas = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    canvas.paste(scene, (0, 0), inner)
    canvas = Image.alpha_composite(canvas, frame)
    return canvas.resize((SIZE, SIZE), Image.LANCZOS)


def frame_image(style):
    """Khung gỗ/vàng bo góc, giữa trống."""
    light, mid, dark = style['frame']
    outer = rounded_mask(0, RADIUS)
    inner_radius = max(RADIUS - BORDER * 0.8, BORDER)
    inner = rounded_mask(BORDER, inner_radius)
    body = vertical_gradient(light, dark)
    # dải sáng giữa: viền trong tối dần
    band = vertical_gradient(lerp(light, mid, 0.4), lerp(mid, dark, 0.7))
    band_mask = rounded_mask(BORDER * 0.22, RADIUS - BORDER * 0.2)
    frame = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    frame.paste(body, (0, 0), outer)
    frame.paste(band, (0, 0), band_mask)
    hole = Image.eval(inner, lambda v: 255 - v)
    ring_alpha = Image.new('L', (S, S), 0)
    ring_alpha.paste(outer, (0, 0), hole)
    frame.putalpha(ring_alpha)
    edge = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(edge).rounded_rectangle((0, 0, S - 1, S - 1), radius=RADIUS, outline=style['edge'] + (255,), width=SS * 2)
    edge_alpha = Image.new('L', (S, S), 0)
    edge_alpha.paste(edge.split()[3], (0, 0), outer)
    edge.putalpha(edge_alpha)
    frame = Image.alpha_composite(frame, edge)
    # vệt sáng cong phía trên
    gloss = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(gloss).rounded_rectangle((BORDER * 0.35, BORDER * 0.3, S - BORDER * 0.35, BORDER * 0.5 + BORDER * 0.4), radius=BORDER * 0.3, fill=(255, 255, 255, 70))
    gloss_alpha = Image.new('L', (S, S), 0)
    gloss_alpha.paste(gloss.split()[3], (0, 0), ring_alpha)
    gloss.putalpha(gloss_alpha)
    return Image.alpha_composite(frame, gloss)


def main(names):
    OUT.mkdir(exist_ok=True)
    for style_name in names:
        style = STYLES[style_name]
        folder = OUT / style_name
        folder.mkdir(exist_ok=True)
        tiles = []
        for color in COLORS:
            for happy in (False, True):
                image = render(style_name, style, color, happy)
                image.save(folder / (f'Avatar_{color}' + ('_Happy' if happy else '') + '.png'))
                tiles.append(image)
        frame_image(style).resize((SIZE, SIZE), Image.LANCZOS).save(folder / f'Frame_{style_name}.png')
        cols = 6
        rows = (len(tiles) + cols - 1) // cols
        sheet = Image.new('RGBA', (cols * (SIZE + 10) + 10, rows * (SIZE + 10) + 10), (70, 70, 80, 255))
        for i, tile in enumerate(tiles):
            sheet.paste(tile, (10 + (i % cols) * (SIZE + 10), 10 + (i // cols) * (SIZE + 10)), tile)
        sheet.save(OUT / f'_Preview_{style_name}.png')
        print('xong', style_name, len(tiles))
    # bảng tổng: mỗi kiểu một hàng, 6 màu mèo
    pick = ['Orange', 'Red', 'Blue', 'Pink', 'Purple', 'Teal']
    size = 160
    sheet = Image.new('RGBA', (len(pick) * (size + 8) + 8, len(names) * (size + 8) + 8), (70, 70, 80, 255))
    for r, style_name in enumerate(names):
        for c, color in enumerate(pick):
            tile = Image.open(OUT / style_name / f'Avatar_{color}.png').resize((size, size), Image.LANCZOS)
            sheet.paste(tile, (8 + c * (size + 8), 8 + r * (size + 8)), tile)
    sheet.save(OUT / '_Preview_AllStyles.png')


if __name__ == '__main__':
    main(sys.argv[1:] or list(STYLES))
