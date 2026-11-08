# Vẽ khối gỗ 3D (nhìn chếch từ trên xuống: thấy mặt trên và mặt trước) cho từng ô của bàn chơi.
# Ghi vào unity/Assets/_Project/Art/Board/. Cần Python 3 + Pillow.
#   python tools/make-board-art.py
# WoodCube, ToxicCube, IceCube: mặt trên 240×240 px + mặt trước 28 px phía dưới (nhập Unity 256 pixel/đơn vị: mặt trên rộng 0,94 ô).
# CubeTop: chỉ hình mặt trên (trắng), làm SpriteMask cho lỗ sáng hướng dẫn. CubeShadow: bóng mờ dưới khối.
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/Board'
SCALE = 4                      # vẽ to rồi thu nhỏ cho mịn cạnh
TOP, FRONT, RADIUS = 240, 28, 30


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def rounded_mask(size, box, radius):
    mask = Image.new('L', size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(box, radius=radius, fill=255)
    return mask


def vertical_gradient(size, top_color, bottom_color):
    w, h = size
    image = Image.new('RGBA', size)
    draw = ImageDraw.Draw(image)
    for y in range(h):
        draw.line([(0, y), (w, y)], fill=lerp(top_color, bottom_color, y / max(h - 1, 1)) + (255,))
    return image


def wood_grain(size, color, seed, count=9):
    rnd = random.Random(seed)
    w, h = size
    layer = Image.new('RGBA', size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for k in range(count):
        y0 = (k + rnd.random() * 0.6) * h / count
        amp, freq, phase = rnd.uniform(4, 12) * SCALE, rnd.uniform(1.2, 2.4), rnd.random() * 6.3
        points = [(x, y0 + math.sin(phase + x / w * freq * math.pi) * amp) for x in range(0, w + 1, 8)]
        draw.line(points, fill=color, width=rnd.choice([2, 3]) * SCALE)
    return layer.filter(ImageFilter.GaussianBlur(SCALE))


def cube(top_colors, front_colors, grain, seed, decorate=None):
    t, f, r = TOP * SCALE, FRONT * SCALE, RADIUS * SCALE
    size = (t, t + f)
    image = Image.new('RGBA', size, (0, 0, 0, 0))
    # Mặt trước: khối bo góc cao bằng cả khối, phần lộ ra là dải dưới mặt trên
    front = vertical_gradient(size, front_colors[0], front_colors[1])
    image.paste(front, (0, 0), rounded_mask(size, (0, 0, t - 1, t + f - 1), r))
    # Mép tối giữa mặt trên và mặt trước
    shade = Image.new('RGBA', size, (0, 0, 0, 0))
    ImageDraw.Draw(shade).rounded_rectangle((0, 8 * SCALE, t - 1, t + 8 * SCALE), radius=r, fill=(40, 20, 8, 70))
    image.alpha_composite(shade.filter(ImageFilter.GaussianBlur(3 * SCALE)))
    image.putalpha(Image.composite(image.getchannel('A'), Image.new('L', size, 0), rounded_mask(size, (0, 0, t - 1, t + f - 1), r)))
    # Mặt trên
    top = vertical_gradient((t, t), top_colors[0], top_colors[1])
    if grain:
        top.alpha_composite(wood_grain((t, t), grain, seed))
    if decorate:
        decorate(top)
    top_mask = rounded_mask((t, t), (0, 0, t - 1, t - 1), r)
    image.paste(top, (0, 0), top_mask)
    # Gờ sáng mép trên và mép trái, gờ tối mép dưới: mặt trên trông như vát cạnh
    bevel = Image.new('RGBA', (t, t), (0, 0, 0, 0))
    draw = ImageDraw.Draw(bevel)
    draw.rounded_rectangle((3 * SCALE, 3 * SCALE, t - 3 * SCALE, t - 3 * SCALE), radius=r - 3 * SCALE,
                           outline=(255, 245, 225, 120), width=4 * SCALE)
    draw.rounded_rectangle((0, 0, t - 1, t - 1), radius=r, outline=(80, 45, 20, 90), width=2 * SCALE)
    image.alpha_composite(bevel.filter(ImageFilter.GaussianBlur(SCALE)), (0, 0))
    return image.resize((TOP, TOP + FRONT), Image.LANCZOS)


# Vũng độc nằm trong mặt trên của khối độc: chiếm 76% x 72% mặt trên, lệch tâm một chút. Là một ảnh riêng để chuyển động trong game
# mà khung khối bên ngoài đứng yên.
POOL_WIDTH, POOL_HEIGHT = round(0.76 * TOP), round(0.72 * TOP)


def goo(top):
    # Khung khối chỉ còn mặt trên phẳng; vũng độc là lớp riêng (ToxicPool.png).
    return


def pool():
    # Vũng độc lõm: bờ sáng ở trên, lòng đậm dần xuống dưới, vài vệt sáng ướt.
    w, h = POOL_WIDTH * SCALE, POOL_HEIGHT * SCALE
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    mask = Image.new('L', (w, h), 0)
    radius = round(0.22 * TOP * SCALE)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=255)
    img.paste(vertical_gradient((w, h), (112, 176, 62), (62, 118, 40)), (0, 0), mask)
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, outline=(74, 128, 44, 255), width=4 * SCALE)
    draw.arc((0, 0, w - 1, h - 1), 200, 330, fill=(206, 240, 150, 220), width=3 * SCALE)
    draw.ellipse((0.14 * w, 0.14 * h, 0.35 * w, 0.20 * h), fill=(225, 252, 190, 190))
    draw.ellipse((0.66 * w, 0.72 * h, 0.80 * w, 0.76 * h), fill=(200, 238, 150, 150))
    return img.resize((POOL_WIDTH, POOL_HEIGHT), Image.LANCZOS)


def frost(top):
    draw = ImageDraw.Draw(top)
    w = top.width
    for x0, y0, length, width in ((0.18, 0.30, 0.16, 7), (0.24, 0.72, 0.36, 9), (0.64, 0.22, 0.10, 6), (0.62, 0.84, 0.12, 5)):
        draw.line([(x0 * w, y0 * w), ((x0 + length) * w, (y0 - length * 0.65) * w)], fill=(255, 255, 255, 210), width=width * SCALE)
    for cx, cy, rr in ((0.78, 0.55, 0.018), (0.35, 0.48, 0.012), (0.55, 0.65, 0.01)):
        draw.ellipse(((cx - rr) * w, (cy - rr) * w, (cx + rr) * w, (cy + rr) * w), fill=(255, 255, 255, 230))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    cube(((236, 186, 124), (214, 152, 92)), ((196, 132, 78), (170, 108, 60)), (150, 95, 50, 70), 11).save(OUT / 'WoodCube.png')
    cube(((150, 205, 92), (104, 168, 62)), ((92, 148, 56), (74, 122, 44)), None, 0, goo).save(OUT / 'ToxicCube.png')
    pool().save(OUT / 'ToxicPool.png')
    cube(((214, 241, 255), (150, 206, 240)), ((150, 196, 228), (124, 172, 210)), None, 0, frost).save(OUT / 'IceCube.png')
    t = TOP * SCALE
    mask = Image.new('RGBA', (t, t), (0, 0, 0, 0))
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, t - 1, t - 1), radius=RADIUS * SCALE, fill=(255, 255, 255, 255))
    mask.resize((TOP, TOP), Image.LANCZOS).save(OUT / 'CubeTop.png')
    pad = 40 * SCALE
    shadow = Image.new('RGBA', (t + 2 * pad, t + 2 * pad), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((pad, pad, pad + t, pad + t), radius=RADIUS * SCALE, fill=(10, 5, 30, 150))
    shadow.filter(ImageFilter.GaussianBlur(14 * SCALE)).resize((TOP + 80, TOP + 80), Image.LANCZOS).save(OUT / 'CubeShadow.png')
    print('saved', OUT)


if __name__ == '__main__':
    main()
