# Vẽ dải ruy băng đỏ cong nhẹ, hai đuôi gấp ra sau (kiểu băng rôn chiến thắng), đặt sau hàng sao của bảng thắng.
# Ghi vào unity/Assets/_Project/Art/UI/WinRibbon.png. Cần Python 3 + Pillow.
#   python tools/make-ribbon.py
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI'
S = 3                                      # vẽ to rồi thu nhỏ cho mịn cạnh
W, H = 1000, 300
FACE_TOP, FACE_BOTTOM = (246, 62, 58), (204, 30, 36)    # mặt trước dải chính
TAIL = (176, 22, 30)                                    # đuôi phía sau
FOLD = (110, 10, 18)                                    # nếp gấp tối giữa thân và đuôi
SHINE = (255, 170, 160, 170)
ARC = 26                                                # độ võng của dải (px)


def bend(x, y):
    """Uốn cong dải: hai đầu thấp hơn giữa một chút, như băng căng hai bên."""
    t = (x / W - 0.5) * 2
    return x * S, (y + ARC * t * t) * S


def poly(points):
    return [bend(x, y) for x, y in points]


def band(x0, x1, top, bottom, steps=40):
    upper = [(x0 + (x1 - x0) * i / steps, top) for i in range(steps + 1)]
    lower = [(x1 - (x1 - x0) * i / steps, bottom) for i in range(steps + 1)]
    return poly(upper + lower)


def gradient_fill(size, mask, top_color, bottom_color, y0, y1):
    image = Image.new('RGBA', size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    for y in range(size[1]):
        t = min(max((y - y0) / max(y1 - y0, 1), 0), 1)
        draw.line([(0, y), (size[0], y)], fill=tuple(round(top_color[i] + (bottom_color[i] - top_color[i]) * t) for i in range(3)) + (255,))
    out = Image.new('RGBA', size, (0, 0, 0, 0))
    out.paste(image, (0, 0), mask)
    return out


def ribbon():
    size = (W * S, H * S)
    image = Image.new('RGBA', size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    top, bottom = 90, 210                 # thân chính
    tail_top, tail_bottom = 120, 240      # đuôi thấp hơn thân
    # Đuôi hai bên: hình chữ nhật có khía chữ V ở đầu ngoài
    for side in (-1, 1):
        if side < 0:
            outer, inner = 20, 170
            shape = [(inner, tail_top), (outer, tail_top), (outer + 48, (tail_top + tail_bottom) / 2), (outer, tail_bottom), (inner, tail_bottom)]
        else:
            outer, inner = W - 20, W - 170
            shape = [(inner, tail_top), (outer, tail_top), (outer - 48, (tail_top + tail_bottom) / 2), (outer, tail_bottom), (inner, tail_bottom)]
        draw.polygon(poly(shape), fill=TAIL + (255,))
        # nếp gấp: tam giác tối nối đuôi với thân
        x = 150 if side < 0 else W - 150
        draw.polygon(poly([(x, bottom), (x + 30 * side, tail_bottom), (x + 30 * side, bottom)]), fill=FOLD + (255,))
    # Thân chính
    mask = Image.new('L', size, 0)
    ImageDraw.Draw(mask).polygon(band(150, W - 150, top, bottom), fill=255)
    image.alpha_composite(gradient_fill(size, mask, FACE_TOP, FACE_BOTTOM, top * S, (bottom + ARC) * S))
    # Viền sáng trên, viền tối dưới
    lines = Image.new('RGBA', size, (0, 0, 0, 0))
    line_draw = ImageDraw.Draw(lines)
    line_draw.line(band(158, W - 158, top + 16, top + 16)[:41], fill=SHINE, width=6 * S)
    line_draw.line(band(150, W - 150, bottom - 7, bottom - 7)[:41], fill=(150, 14, 22, 220), width=5 * S)
    image.alpha_composite(lines)
    # Bóng đổ mềm phía dưới
    shadow = Image.new('RGBA', size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).polygon(band(170, W - 170, bottom + 6, bottom + 22), fill=(40, 0, 20, 90))
    shadow = shadow.filter(ImageFilter.GaussianBlur(8 * S))
    shadow.alpha_composite(image)
    return shadow.resize((W, H), Image.LANCZOS)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    ribbon().save(OUT / 'WinRibbon.png')
    print('saved', OUT / 'WinRibbon.png')
