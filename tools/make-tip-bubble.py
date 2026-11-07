# Vẽ bong bóng lời thoại kiểu bảng gỗ (cùng chất liệu với khối gỗ của bàn chơi) và đuôi nhọn.
# Ghi vào unity/Assets/_Project/Art/UI/TipWood.png và TipWoodTail.png. Cần Python 3 + Pillow.
#   python tools/make-tip-bubble.py
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI'
SCALE = 3
WIDTH, HEIGHT, LIP, RADIUS = 1000, 270, 22, 54
TOP = (240, 192, 132)       # mặt gỗ, cùng tông WoodCube
BOTTOM = (220, 160, 100)
EDGE = (150, 90, 45)        # gờ dưới và đuôi
GRAIN = (196, 134, 76, 120)
RIM = (255, 230, 186, 170)  # viền sáng bên trong


def gradient(w, h, top, bottom):
    image = Image.new('RGBA', (w, h))
    draw = ImageDraw.Draw(image)
    for y in range(h):
        t = y / max(h - 1, 1)
        draw.line([(0, y), (w, y)], fill=tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return image


def plank():
    w, h, lip, r = WIDTH * SCALE, HEIGHT * SCALE, LIP * SCALE, RADIUS * SCALE
    face_h = h - lip
    image = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(image).rounded_rectangle((0, lip, w - 1, h - 1), radius=r, fill=EDGE + (255,))
    face_mask = Image.new('L', (w, h), 0)
    ImageDraw.Draw(face_mask).rounded_rectangle((0, 0, w - 1, face_h - 1), radius=r, fill=255)
    face = gradient(w, h, TOP, BOTTOM)
    # vân gỗ: các đường lượn ngang
    rnd = random.Random(7)
    grain = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(grain)
    for k in range(7):
        y0 = (k + 0.5 + rnd.random() * 0.4) * face_h / 7
        amp, freq, phase = rnd.uniform(4, 10) * SCALE, rnd.uniform(1.0, 2.0), rnd.random() * 6.3
        draw.line([(x, y0 + math.sin(phase + x / w * freq * math.pi) * amp) for x in range(0, w + 1, 12)], fill=GRAIN, width=3 * SCALE)
    face.alpha_composite(grain.filter(ImageFilter.GaussianBlur(SCALE)))
    image.paste(face, (0, 0), face_mask)
    rim = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    inset = 9 * SCALE
    ImageDraw.Draw(rim).rounded_rectangle((inset, inset, w - 1 - inset, face_h - 1 - inset), radius=r - inset, outline=RIM, width=4 * SCALE)
    image.alpha_composite(rim)
    return image.resize((WIDTH, HEIGHT), Image.LANCZOS)


def tail():
    w, h = 90 * SCALE, 80 * SCALE
    image = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(image).polygon([(0.12 * w, 0), (0.95 * w, 0), (0.08 * w, h)], fill=EDGE + (255,))
    return image.resize((90, 80), Image.LANCZOS)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    plank().save(OUT / 'TipWood.png')
    tail().save(OUT / 'TipWoodTail.png')
    print('saved', OUT)
