# Vẽ nền màn chơi "Hồ lá súng": mặt nước xanh lục có gợn sáng, lá súng và hoa sen quanh mép màn hình.
# Ghi vào unity/Assets/_Project/Art/UI/BackgroundLake.png (900×1950, dọc như điện thoại). Cần Python 3 + Pillow.
#   python tools/make-backgrounds.py
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI'
W, H = 900, 1950
WATER_TOP, WATER_BOTTOM = (78, 176, 170), (36, 112, 128)
SHINE = (200, 255, 240)
PAD, PAD_EDGE = (84, 168, 92), (52, 120, 62)
LOTUS, LOTUS_EDGE, LOTUS_HEART = (255, 190, 214), (232, 130, 170), (255, 226, 120)
# Lá súng nằm sát mép trái, phải để chừa giữa màn hình cho bàn chơi
PADS = [(70, 300), (820, 520), (60, 1200), (840, 1450), (120, 1760), (780, 240), (850, 1000), (40, 760)]


def water():
    image = Image.new('RGB', (W, H))
    draw = ImageDraw.Draw(image)
    for y in range(H):
        t = y / (H - 1)
        draw.line([(0, y), (W, y)], fill=tuple(round(WATER_TOP[i] + (WATER_BOTTOM[i] - WATER_TOP[i]) * t) for i in range(3)))
    return image


def shine(image, seed=5):
    # gợn sáng: nhiều đường sin chồng chéo, làm mờ nhẹ
    rnd = random.Random(seed)
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(70):
        y0, amp, period, phase = rnd.uniform(-50, H + 50), rnd.uniform(10, 26), rnd.uniform(140, 260), rnd.random() * 6.28
        points = [(x, y0 + math.sin(x / period * 6.28 + phase) * amp + math.sin(x / (period * .43) + phase * 2) * amp * .35)
                  for x in range(-20, W + 21, 12)]
        draw.line(points, fill=SHINE + (60,), width=4)
    image = image.convert('RGBA')
    image.alpha_composite(layer.filter(ImageFilter.GaussianBlur(2)))
    return image.convert('RGB')


def lilies(image, seed=6):
    rnd = random.Random(seed)
    draw = ImageDraw.Draw(image)
    for x, y in PADS:
        r, angle = rnd.uniform(55, 80), rnd.uniform(0, 360)
        draw.pieslice((x - r, y - r * .8, x + r, y + r * .8), angle + 20, angle + 360, fill=PAD, outline=PAD_EDGE, width=4)
        if rnd.random() < .5:
            for k in range(6):
                t = k / 6 * 6.28
                px, py = x + math.cos(t) * 14, y - 6 + math.sin(t) * 10
                draw.ellipse((px - 12, py - 9, px + 12, py + 9), fill=LOTUS, outline=LOTUS_EDGE, width=2)
            draw.ellipse((x - 7, y - 13, x + 7, y + 1), fill=LOTUS_HEART)
    return image


def vignette(image, strength=.25):
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).ellipse((-W * .3, -H * .15, W * 1.3, H * 1.15), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(160))
    return Image.composite(image, Image.blend(image, Image.new('RGB', (W, H)), strength), mask)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    vignette(lilies(shine(water()))).save(OUT / 'BackgroundLake.png')
    print('saved', OUT / 'BackgroundLake.png')
