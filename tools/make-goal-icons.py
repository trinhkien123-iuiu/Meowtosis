# Icon hộp trung tính cho viên mục tiêu trên HUD: lấy dáng hộp Panel_Box_Red (Shikacat, tạm) rồi đổi sang tông kem,
# để icon không gợi màu mèo nào (mục tiêu đếm mọi hộp).
#   python tools/make-goal-icons.py
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art'
DARK, LIGHT = (122, 78, 46), (255, 244, 222)


def recolor(source):
    rgba = source.convert('RGBA')
    gray = rgba.convert('L')
    lo, hi = gray.getextrema()
    span = max(hi - lo, 1)
    out = Image.new('RGBA', rgba.size)
    src, g, dst = rgba.load(), gray.load(), out.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            t = (g[x, y] - lo) / span
            dst[x, y] = tuple(round(DARK[i] + (LIGHT[i] - DARK[i]) * t) for i in range(3)) + (src[x, y][3],)
    return out


if __name__ == '__main__':
    icon = recolor(Image.open(ROOT / 'Shikacat/Cats/Panel_Box_Red.png'))
    icon.thumbnail((128, 128), Image.LANCZOS)
    icon.save(ROOT / 'UI/GoalBox.png')
    print('saved GoalBox.png', icon.size)
