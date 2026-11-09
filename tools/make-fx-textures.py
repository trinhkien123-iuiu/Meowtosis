# Tạo texture hạt hiệu ứng tự vẽ cho các cơ chế (không dùng asset ripped).
#   python tools/make-fx-textures.py
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/Effects'
BOARD = OUT.parent / 'Board'
SIZE = 128


def bubble():
    # Bong bóng: lòng trong suốt nhẹ, viền sáng mỏng, chấm sáng ở góc trên trái, vệt sáng nhỏ ở góc dưới phải.
    scale = 4
    big = Image.new('RGBA', (SIZE * scale, SIZE * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(big)
    pad = 6 * scale
    box = (pad, pad, SIZE * scale - pad, SIZE * scale - pad)
    draw.ellipse(box, fill=(255, 255, 255, 46))
    draw.ellipse(box, outline=(255, 255, 255, 235), width=5 * scale)
    # chấm sáng lớn trên trái
    draw.ellipse((30 * scale, 24 * scale, 56 * scale, 44 * scale), fill=(255, 255, 255, 235))
    # vệt sáng nhỏ dưới phải
    draw.arc((22 * scale, 22 * scale, 106 * scale, 106 * scale), 20, 60, fill=(255, 255, 255, 170), width=5 * scale)
    return big.resize((SIZE, SIZE), Image.LANCZOS)


def gas():
    # Đám khí: nhiều vòng tròn mờ chồng nhau, làm nhoè mép.
    img = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for cx, cy, r in [(64, 64, 40), (46, 56, 28), (84, 58, 30), (60, 84, 26), (70, 46, 24)]:
        draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255, 110))
    return img.filter(ImageFilter.GaussianBlur(10))


def glint():
    # Tia lấp lánh bốn cánh nhọn, lõi sáng.
    scale = 4
    img = Image.new('RGBA', (SIZE * scale, SIZE * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = SIZE * scale // 2
    long, short = 58 * scale, 7 * scale
    draw.polygon([(c, c - long), (c + short, c - short), (c + long, c), (c + short, c + short),
                  (c, c + long), (c - short, c + short), (c - long, c), (c - short, c - short)], fill=(255, 255, 255, 255))
    out = img.resize((SIZE, SIZE), Image.LANCZOS)
    glow = out.filter(ImageFilter.GaussianBlur(5))
    return Image.alpha_composite(glow, out)


def toxic_aura():
    # Quầng độc quanh ô độc: hình vuông bo tròn xanh lục, nhoè dần ra ngoài (nằm dưới khối, chỉ lộ phần ngoài mép).
    img = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
    ImageDraw.Draw(img).rounded_rectangle((52, 52, 204, 204), radius=36, fill=(120, 230, 70, 255))
    return img.filter(ImageFilter.GaussianBlur(26))


def portal_swirl():
    # Vòng xoáy của cổng dịch chuyển: ba nhánh xoắn ốc trắng, mỏng dần ra ngoài, lõi sáng. Tô màu trong game.
    import math
    scale = 3
    size = 256 * scale
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size / 2
    for arm in range(3):
        base = arm * 2 * math.pi / 3
        points = []
        for step in range(0, 260):
            t = step / 259
            angle = base + t * 3.2 * math.pi
            radius = (0.06 + 0.9 * t) * c * 0.96
            points.append((c + radius * math.cos(angle), c + radius * math.sin(angle)))
        for k in range(len(points) - 1):
            t = k / len(points)
            width = max(2, round((16 - 11 * t) * scale / 2))
            alpha = round(255 * (0.95 - 0.55 * t))
            draw.line([points[k], points[k + 1]], fill=(255, 255, 255, alpha), width=width)
    draw.ellipse((c - 26 * scale, c - 26 * scale, c + 26 * scale, c + 26 * scale), fill=(255, 255, 255, 255))
    out = img.resize((256, 256), Image.LANCZOS)
    glow = out.filter(ImageFilter.GaussianBlur(7))
    return Image.alpha_composite(glow, out)


def portal_glow():
    # Quầng sáng mềm sau cổng: đốm tròn nhoè dần ra ngoài.
    img = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse((40, 40, 216, 216), fill=(255, 255, 255, 255))
    return img.filter(ImageFilter.GaussianBlur(34))


def badge_disc():
    # Đĩa tròn cho huy hiệu mèo hai màu: trắng, sáng hơn ở trên, tối nhẹ ở dưới; dùng cho viền (trắng) và nền (tô màu kia).
    scale = 4
    size = 128 * scale
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pad = 4 * scale
    draw.ellipse((pad, pad, size - pad, size - pad), fill=(255, 255, 255, 255))
    shade = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(shade).ellipse((pad, pad + size * 0.18, size - pad, size - pad + size * 0.1), fill=(0, 0, 0, 60))
    shade = shade.filter(ImageFilter.GaussianBlur(10 * scale / 2))
    mask = Image.new('L', (size, size), 0)
    ImageDraw.Draw(mask).ellipse((pad, pad, size - pad, size - pad), fill=255)
    clipped = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    clipped.paste(shade, (0, 0), mask)
    img = Image.alpha_composite(img, clipped)
    ImageDraw.Draw(img).ellipse((size * 0.26, size * 0.14, size * 0.55, size * 0.3), fill=(255, 255, 255, 170))
    return img.resize((128, 128), Image.LANCZOS)


def color_aura():
    # Vòng hào quang dưới chân mèo hai màu: vành sáng nhoè, rỗng ở giữa. Tô bằng màu kia trong game.
    img = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.ellipse((26, 26, 230, 230), outline=(255, 255, 255, 255), width=18)
    soft = img.filter(ImageFilter.GaussianBlur(9))
    return Image.alpha_composite(soft, img.filter(ImageFilter.GaussianBlur(1.5)))


def magic_outer():
    # Vòng ma thuật ngoài dưới chân mèo (ô được chọn khi dùng booster): hai vành tròn, giữa là dải ký tự (gạch, chấm, tam giác nhỏ).
    import math
    scale = 2
    size = 256 * scale
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size / 2
    for radius, width in ((118, 5), (96, 3)):
        r = radius * scale
        draw.ellipse((c - r, c - r, c + r, c + r), outline=(255, 255, 255, 255), width=width * scale)
    for index in range(24):
        a = index / 24 * math.tau
        r = 107 * scale
        x, y = c + math.cos(a) * r, c + math.sin(a) * r
        if index % 3 == 0:
            s = 7 * scale
            points = [(x + math.cos(a) * s, y + math.sin(a) * s), (x + math.cos(a + 2.2) * s, y + math.sin(a + 2.2) * s), (x + math.cos(a - 2.2) * s, y + math.sin(a - 2.2) * s)]
            draw.polygon(points, fill=(255, 255, 255, 255))
        elif index % 3 == 1:
            s = 3 * scale
            draw.ellipse((x - s, y - s, x + s, y + s), fill=(255, 255, 255, 230))
        else:
            dx, dy = -math.sin(a) * 6 * scale, math.cos(a) * 6 * scale
            draw.line((x - dx, y - dy, x + dx, y + dy), fill=(255, 255, 255, 230), width=3 * scale)
    img = img.resize((256, 256), Image.LANCZOS)
    return Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(4)), img)


def magic_inner():
    # Lõi vòng ma thuật: ngôi sao sáu cánh trong một vành tròn, xoay ngược chiều với vòng ngoài.
    import math
    scale = 2
    size = 256 * scale
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size / 2
    r = 78 * scale
    draw.ellipse((c - r, c - r, c + r, c + r), outline=(255, 255, 255, 255), width=4 * scale)
    for start in (0, math.pi / 3):
        points = [(c + math.cos(start + k * math.tau / 3 - math.pi / 2) * r, c + math.sin(start + k * math.tau / 3 - math.pi / 2) * r) for k in range(3)]
        draw.polygon(points, outline=(255, 255, 255, 255), width=3 * scale)
    s = 9 * scale
    draw.ellipse((c - s, c - s, c + s, c + s), fill=(255, 255, 255, 255))
    img = img.resize((256, 256), Image.LANCZOS)
    return Image.alpha_composite(img.filter(ImageFilter.GaussianBlur(3)), img)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    magic_outer().save(BOARD / 'MagicCircleOuter.png')
    magic_inner().save(BOARD / 'MagicCircleInner.png')
    print('saved MagicCircleOuter, MagicCircleInner')
    for name, make in (('ParticleBubble', bubble), ('ParticleGas', gas), ('ParticleGlint', glint)):
        make().save(OUT / f'{name}.png')
        print('saved', name)
    BOARD.mkdir(parents=True, exist_ok=True)
    toxic_aura().save(BOARD / 'ToxicAura.png')
    bubble().save(BOARD / 'ToxicBubble.png')
    for name, make in (('PortalSwirl', portal_swirl), ('PortalGlow', portal_glow), ('BadgeDisc', badge_disc), ('ColorAura', color_aura)):
        make().save(BOARD / f'{name}.png')
    print('saved ToxicAura, ToxicBubble, PortalSwirl, PortalGlow, BadgeDisc, ColorAura')
