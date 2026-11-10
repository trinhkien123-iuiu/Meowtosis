"""Bộ hình minh hoạ chèn trong lời hướng dẫn (bong bóng chat), ghép thành một ảnh ngang 8 ô 128x128:
red, blue, yellow (đầu mèo Shikacat), poof (đám khói), arrow (mũi tên), empty (ô gỗ trống), check (dấu tích), cross (dấu X).
Ghi ra unity/Assets/_Project/Art/UI/Pictograms.png; Unity cắt thành sprite cùng tên và dựng TMP Sprite Asset từ đó.
  python tools/make-pictograms.py"""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ART = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art'
CELL = 128
NAMES = ['red', 'blue', 'yellow', 'poof', 'arrow', 'empty', 'check', 'cross']


def fit(img, box=CELL - 8):
    img = img.crop(img.getbbox() or (0, 0, img.width, img.height))
    scale = box / max(img.width, img.height)            # phóng hoặc thu cho vừa ô (ảnh gốc nhỏ như dấu X cũng to lên)
    img = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    out = Image.new('RGBA', (CELL, CELL), (0, 0, 0, 0))
    out.paste(img, ((CELL - img.width) // 2, (CELL - img.height) // 2), img)
    return out


def cat_head(color):
    cat = Image.open(ART / 'Shikacat/Cats' / f'Image_Cat_{color}.png').convert('RGBA')
    return fit(cat.crop((0, 0, cat.width, int(cat.height * 0.62))))


def poof():
    S = CELL * 4
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    puffs = [(0.5, 0.56, 0.24), (0.3, 0.6, 0.17), (0.7, 0.6, 0.17), (0.4, 0.4, 0.17), (0.62, 0.4, 0.15)]
    for x, y, r in puffs:
        d.ellipse(((x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S), fill=(120, 110, 120, 255))
    for x, y, r in puffs:
        r2 = r - 0.025
        d.ellipse(((x - r2) * S, (y - r2) * S, (x + r2) * S, (y + r2) * S), fill=(245, 242, 250, 255))
    for k in range(6):
        a = k * math.pi / 3 + 0.3
        x, y = 0.5 + math.cos(a) * 0.42, 0.5 + math.sin(a) * 0.42
        d.ellipse(((x - 0.035) * S, (y - 0.035) * S, (x + 0.035) * S, (y + 0.035) * S), fill=(255, 220, 90, 255))
    return img.resize((CELL, CELL), Image.LANCZOS)


def arrow():
    S = CELL * 4
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = [(0.14, 0.4), (0.56, 0.4), (0.56, 0.24), (0.88, 0.5), (0.56, 0.76), (0.56, 0.6), (0.14, 0.6)]
    d.polygon([(x * S, y * S) for x, y in pts], fill=(110, 60, 30, 255))
    inner = [(0.19, 0.44), (0.6, 0.44), (0.6, 0.33), (0.81, 0.5), (0.6, 0.67), (0.6, 0.56), (0.19, 0.56)]
    d.polygon([(x * S, y * S) for x, y in inner], fill=(255, 196, 80, 255))
    return img.resize((CELL, CELL), Image.LANCZOS)


def empty_tile():
    tile = Image.open(ART / 'Board/WoodCube.png').convert('RGBA')
    return fit(tile, CELL - 18)


def from_file(path, box=CELL - 16):
    return fit(Image.open(path).convert('RGBA'), box)


def main():
    icons = {
        'red': cat_head('Red'), 'blue': cat_head('Blue'), 'yellow': cat_head('Yellow'),
        'poof': poof(), 'arrow': arrow(), 'empty': empty_tile(),
        'check': from_file(ART / 'UI/IconCheck.png'), 'cross': from_file(ART / 'UI/Bounce/BounceClose.png', CELL - 10),
    }
    sheet = Image.new('RGBA', (CELL * len(NAMES), CELL), (0, 0, 0, 0))
    for index, name in enumerate(NAMES):
        sheet.paste(icons[name], (index * CELL, 0), icons[name])
    out = ART / 'UI/Pictograms.png'
    sheet.save(out)
    print('ghi', out)


if __name__ == '__main__':
    main()
