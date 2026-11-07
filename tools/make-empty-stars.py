# Tạo sao trống từ chính hình sao đạt được, để hai trạng thái khớp dáng và cỡ: giữ alpha và độ sáng tối của các mặt khối,
# đổi màu sang tông nâu xám (như ô sao chưa có), hơi trong để chìm vào nền.
#   python tools/make-empty-stars.py
from pathlib import Path

from PIL import Image

ART = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/Catdom'
PAIRS = [('WinStarLit.png', 'WinStarEmpty.png'), ('StarMeterLit.png', 'StarMeterEmpty.png')]
DARK, LIGHT = (92, 62, 52), (178, 150, 132)   # tông nâu xám: mặt tối nhất và sáng nhất
OPACITY = 0.9


def empty_from(lit):
    rgba = lit.convert('RGBA')
    gray = rgba.convert('L')
    lo, hi = gray.getextrema()
    span = max(hi - lo, 1)
    out = Image.new('RGBA', rgba.size)
    src, g = rgba.load(), gray.load()
    dst = out.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            t = (g[x, y] - lo) / span
            alpha = round(src[x, y][3] * OPACITY)
            dst[x, y] = tuple(round(DARK[i] + (LIGHT[i] - DARK[i]) * t) for i in range(3)) + (alpha,)
    return out


if __name__ == '__main__':
    for lit_name, empty_name in PAIRS:
        empty_from(Image.open(ART / lit_name)).save(ART / empty_name)
        print('saved', empty_name)
