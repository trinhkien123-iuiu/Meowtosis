# Vẽ hình tạm cho giao diện kiểu game mobile: nút tròn booster, huy hiệu, panel viền vàng, nút, thanh sao, bong bóng,
# icon. Icon booster/gợi ý/cài đặt vẽ từ emoji Segoe UI Emoji (giống prototype dùng emoji); chỉ là hình tạm cho
# dự án cá nhân, phải thay trước khi phát hành. Ghi vào unity/Assets/_Project/Art/UI/.
# Ảnh viền 9 phần (panel, nút...) ghi kèm cỡ viền vào tools/ui-slices.txt (trái dưới phải trên); Sprite Border
# trong file .meta của từng ảnh đặt theo các số này.
#   python tools/make-ui-art.py
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI'
SUPERSAMPLE = 4
EMOJI_FONT = 'C:/Windows/Fonts/seguiemj.ttf'

# Cỡ viền 9 phần (trái, dưới, phải, trên) tính bằng pixel ảnh xuất ra.
SLICES = {}


def rgba(color, opacity=1.0):
    color = color.lstrip('#')
    return tuple(int(color[i:i + 2], 16) for i in (0, 2, 4)) + (round(255 * opacity),)


class Canvas:
    def __init__(self, width, height):
        self.width, self.height = width, height
        self.image = Image.new('RGBA', (width * SUPERSAMPLE, height * SUPERSAMPLE), (0, 0, 0, 0))

    def box(self, left, top, right, bottom):
        return tuple(round(v * SUPERSAMPLE) for v in (left, top, right, bottom))

    def mask(self, draw_shape):
        mask = Image.new('L', self.image.size, 0)
        draw_shape(ImageDraw.Draw(mask))
        return mask

    def fill(self, mask, top_color, bottom_color=None, radial=False, opacity=1.0):
        """Tô màu (hoặc gradient dọc / tròn) vào vùng mask."""
        height, width = mask.size[1], mask.size[0]
        bottom_color = bottom_color or top_color
        ys, xs = np.mgrid[0:height, 0:width].astype(np.float32)
        columns = np.where(np.array(mask) > 0)
        if len(columns[0]) == 0:
            return
        top, bottom = columns[0].min(), columns[0].max()
        left, right = columns[1].min(), columns[1].max()
        if radial:
            cx, cy = (left + right) / 2, top + (bottom - top) * 0.35
            radius = max(right - left, bottom - top) * 0.75
            t = np.clip(np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2) / radius, 0, 1)
        else:
            t = np.clip((ys - top) / max(bottom - top, 1), 0, 1)
        start, end = np.array(rgba(top_color)[:3], np.float32), np.array(rgba(bottom_color)[:3], np.float32)
        channels = [start[c] + (end[c] - start[c]) * t for c in range(3)]
        alpha = np.array(mask, np.float32) * opacity
        layer = Image.fromarray(np.dstack(channels + [alpha]).astype(np.uint8), 'RGBA')
        self.image = Image.alpha_composite(self.image, layer)

    def rounded(self, left, top, right, bottom, radius, top_color, bottom_color=None, radial=False, opacity=1.0):
        mask = self.mask(lambda d: d.rounded_rectangle(self.box(left, top, right, bottom), radius=radius * SUPERSAMPLE, fill=255))
        self.fill(mask, top_color, bottom_color, radial, opacity)

    def circle(self, cx, cy, r, top_color, bottom_color=None, radial=False, opacity=1.0):
        mask = self.mask(lambda d: d.ellipse(self.box(cx - r, cy - r, cx + r, cy + r), fill=255))
        self.fill(mask, top_color, bottom_color, radial, opacity)

    def polygon(self, points, color):
        layer = Image.new('RGBA', self.image.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).polygon([(x * SUPERSAMPLE, y * SUPERSAMPLE) for x, y in points], fill=rgba(color))
        self.image = Image.alpha_composite(self.image, layer)

    def line(self, points, color, width):
        layer = Image.new('RGBA', self.image.size, (0, 0, 0, 0))
        draw = ImageDraw.Draw(layer)
        scaled = [(x * SUPERSAMPLE, y * SUPERSAMPLE) for x, y in points]
        draw.line(scaled, fill=rgba(color), width=round(width * SUPERSAMPLE), joint='curve')
        r = width * SUPERSAMPLE / 2
        for x, y in (scaled[0], scaled[-1]):
            draw.ellipse((x - r, y - r, x + r, y + r), fill=rgba(color))
        self.image = Image.alpha_composite(self.image, layer)

    def paste(self, image, left, top):
        self.image.alpha_composite(image.resize((image.width * SUPERSAMPLE, image.height * SUPERSAMPLE), Image.LANCZOS),
                                   (round(left * SUPERSAMPLE), round(top * SUPERSAMPLE)))

    def save(self, name, border=None):
        OUT.mkdir(parents=True, exist_ok=True)
        self.image.resize((self.width, self.height), Image.LANCZOS).save(OUT / f'{name}.png')
        if border:
            SLICES[name] = border
        print('ghi', name)


def framed_panel(name, size, radius, rim, fill_top, fill_bottom, rim_top='#ffe27a', rim_bottom='#f0a020', outline='#6b3a00'):
    """Panel bo góc: viền ngoài sẫm, vành vàng, lòng gradient, vệt sáng phía trên."""
    width, height = size
    canvas = Canvas(width, height)
    canvas.rounded(0, 0, width, height, radius, outline)
    canvas.rounded(3, 3, width - 3, height - 3, radius - 3, rim_top, rim_bottom)
    canvas.rounded(3 + rim, 3 + rim, width - 3 - rim, height - 3 - rim, radius - 3 - rim, fill_top, fill_bottom)
    canvas.rounded(3 + rim + 6, 3 + rim + 4, width - 3 - rim - 6, 3 + rim + 4 + (height - 2 * rim) * 0.18,
                   max(4, radius - rim - 8), '#ffffff', opacity=0.18)
    border = radius + 6
    canvas.save(name, (border, border, border, border))


def button(name, top_color, bottom_color, lip_color):
    """Nút chữ nhật bo tròn: viền cam, thân gradient, môi dưới sẫm, vệt bóng."""
    width, height, radius = 192, 128, 40
    canvas = Canvas(width, height)
    canvas.rounded(0, 0, width, height, radius, '#8a4a00')
    canvas.rounded(4, 4, width - 4, height - 4, radius - 4, '#ffd34d', '#ff9a1a')
    canvas.rounded(10, 10, width - 10, height - 10, radius - 10, lip_color)
    canvas.rounded(10, 10, width - 10, height - 18, radius - 10, top_color, bottom_color)
    canvas.rounded(22, 16, width - 22, 40, 14, '#ffffff', opacity=0.35)
    canvas.save(name, (radius + 8, radius + 8, radius + 8, radius + 8))


def round_button():
    size = 256
    canvas = Canvas(size, size)
    c = size / 2
    canvas.circle(c, c + 6, 124, '#0b1240', opacity=0.5)          # bóng đổ
    canvas.circle(c, c, 124, '#3a1c00')
    canvas.circle(c, c, 118, '#ffe066', '#e2840f')                 # vành vàng
    canvas.circle(c, c, 98, '#1a1f6e')
    canvas.circle(c, c, 94, '#4b5cf0', '#1d2380', radial=True)     # lòng xanh
    canvas.rounded(c - 62, c - 84, c + 62, c - 40, 22, '#ffffff', opacity=0.16)
    canvas.save('RoundButton')


def badge(name, fill_top, fill_bottom, rim):
    size = 96
    canvas = Canvas(size, size)
    c = size / 2
    canvas.circle(c, c, 46, '#14184d')
    canvas.circle(c, c, 42, rim)
    canvas.circle(c, c, 35, fill_top, fill_bottom)
    if name == 'BadgePlus':
        canvas.line([(c - 18, c), (c + 18, c)], '#14530a', 15)
        canvas.line([(c, c - 18), (c, c + 18)], '#14530a', 15)
        canvas.line([(c - 17, c), (c + 17, c)], '#ffffff', 10)
        canvas.line([(c, c - 17), (c, c + 17)], '#ffffff', 10)
    canvas.save(name)


def emoji_icon(name, codepoint, size=256, fill=0.86):
    font = ImageFont.truetype(EMOJI_FONT, 109)
    glyph = Image.new('RGBA', (160, 160), (0, 0, 0, 0))
    ImageDraw.Draw(glyph).text((80, 80), chr(codepoint), font=font, embedded_color=True, anchor='mm')
    glyph = glyph.crop(glyph.getbbox())
    scale = size * fill / max(glyph.size)
    glyph = glyph.resize((round(glyph.width * scale), round(glyph.height * scale)), Image.LANCZOS)
    # viền sẫm mỏng để icon nổi trên nền xanh
    shadow = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    left, top = (size - glyph.width) // 2, (size - glyph.height) // 2
    outline = Image.new('RGBA', glyph.size, rgba('#10164f'))
    outline.putalpha(glyph.getchannel('A'))
    shadow.alpha_composite(outline, (left, top + 6))
    shadow = shadow.filter(ImageFilter.GaussianBlur(3))
    shadow.alpha_composite(glyph, (left, top))
    OUT.mkdir(parents=True, exist_ok=True)
    shadow.save(OUT / f'{name}.png')
    print('ghi', name)
    return shadow


def undo_icon():
    # Mũi tên vòng ngược chiều kim đồng hồ: đầu mũi tên ở đỉnh, chỉ sang trái.
    import math
    size = 256
    canvas = Canvas(size, size)
    cx, cy, r = size / 2 + 10, size / 2 + 12, 66
    arc = [(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))) for a in range(-90, 151, 5)]
    head = [(cx - 58, cy - r), (cx - 4, cy - r - 40), (cx - 4, cy - r + 40)]
    canvas.line(arc, '#10164f', 46)
    canvas.polygon([(x - 6, y) for x, y in [(cx - 64, cy - r), (cx + 2, cy - r - 48), (cx + 2, cy - r + 48)]], '#10164f')
    canvas.line(arc, '#ffffff', 30)
    canvas.polygon(head, '#ffffff')
    canvas.save('IconUndo')


def coin_icon():
    import math
    size = 128
    canvas = Canvas(size, size)
    c = size / 2
    canvas.circle(c, c + 3, 58, '#8a4a00')
    canvas.circle(c, c, 58, '#b86b00')
    canvas.circle(c, c, 52, '#ffe680', '#f5a700')
    canvas.circle(c, c, 40, '#f7b51a', '#ffd84d')
    star = []
    for k in range(10):
        r = 26 if k % 2 == 0 else 11
        a = math.radians(-90 + k * 36)
        star.append((c + r * math.cos(a), c + 2 + r * math.sin(a)))
    canvas.polygon(star, '#d98a00')
    canvas.rounded(c - 30, c - 48, c + 20, c - 36, 6, '#ffffff', opacity=0.5)
    canvas.save('IconCoin')


def close_icon():
    size = 128
    canvas = Canvas(size, size)
    c = size / 2
    canvas.circle(c, c, 60, '#5a0a14')
    canvas.circle(c, c, 54, '#ff7b8a', '#e0213c')
    canvas.line([(c - 22, c - 22), (c + 22, c + 22)], '#ffffff', 16)
    canvas.line([(c + 22, c - 22), (c - 22, c + 22)], '#ffffff', 16)
    canvas.save('IconClose')


def star_empty(star):
    grey = star.convert('LA').convert('RGBA')
    pixels = np.array(grey, np.float32)
    pixels[..., :3] = pixels[..., :3] * 0.45 + 40
    Image.fromarray(pixels.astype(np.uint8), 'RGBA').save(OUT / 'IconStarEmpty.png')
    print('ghi IconStarEmpty')


def bar(name, top_color, bottom_color, rim):
    width, height, radius = 128, 48, 24
    canvas = Canvas(width, height)
    canvas.rounded(0, 0, width, height, radius, rim)
    canvas.rounded(4, 4, width - 4, height - 4, radius - 4, top_color, bottom_color)
    canvas.rounded(12, 8, width - 12, 18, 5, '#ffffff', opacity=0.3)
    canvas.save(name, (radius + 2, radius, radius + 2, radius))


def bubble():
    width, height, radius = 128, 128, 36
    canvas = Canvas(width, height)
    canvas.rounded(0, 0, width, height, radius, '#14184d')
    canvas.rounded(5, 5, width - 5, height - 5, radius - 5, '#ffffff', '#eef1ff')
    canvas.save('Bubble', (radius + 6, radius + 6, radius + 6, radius + 6))
    tail = Canvas(64, 48)
    tail.polygon([(4, 0), (60, 0), (14, 46)], '#14184d')
    tail.polygon([(12, 0), (50, 0), (17, 34)], '#ffffff')
    tail.save('BubbleTail')


def pill():
    width, height, radius = 96, 72, 36
    canvas = Canvas(width, height)
    canvas.rounded(0, 0, width, height, radius, '#0a1450', opacity=0.75)
    canvas.rounded(3, 3, width - 3, height - 3, radius - 3, '#1b2a8a', '#111c66', opacity=0.85)
    canvas.save('Pill', (radius, radius, radius, radius))


def rays():
    import math
    # Tia sáng mềm: 14 tia, độ sáng đổi mượt theo góc, mờ dần về tâm và về mép.
    size = 512
    axis = np.linspace(-1, 1, size, dtype=np.float32)
    x, y = np.meshgrid(axis, axis)
    radius = np.sqrt(x * x + y * y)
    angle = np.arctan2(y, x)
    beam = np.clip(0.5 + 0.5 * np.cos(angle * 14), 0, 1) ** 3
    fade = np.clip(radius / 0.12, 0, 1) * np.clip((1 - radius) / 0.65, 0, 1)
    alpha = (beam * fade * 0.55 * 255).astype(np.uint8)
    canvas = Canvas(size, size)
    canvas.image = Image.merge('RGBA', (*[Image.new('L', (size, size), 255)] * 3, Image.fromarray(alpha)))
    canvas.save('Rays')
    # Quầng sáng tròn mờ dần về 0 ở mép (vẽ hình tròn nhỏ hơn khung rồi làm mờ).
    soft = Canvas(256, 256)
    width = soft.image.size[0]
    mask = Image.new('L', soft.image.size, 0)
    ImageDraw.Draw(mask).ellipse((width * 0.22, width * 0.22, width * 0.78, width * 0.78), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(width / 9))
    soft.fill(mask, '#ffffff')
    soft.save('Glow')


def stamp_paw(draw, size):
    # Chân mèo vẽ trong khung vuông `size`, tâm ở giữa khung.
    center = size / 2
    unit = size / 100
    draw.ellipse((center - 26 * unit, center - 6 * unit, center + 26 * unit, center + 36 * unit), fill=255)
    for x, y, half_width, half_height in ((-34, -8, 11, 14), (-13, -28, 11, 15), (13, -28, 11, 15), (34, -8, 11, 14)):
        draw.ellipse((center + (x - half_width) * unit, center + (y - half_height) * unit,
                      center + (x + half_width) * unit, center + (y + half_height) * unit), fill=255)


def stamp_cat(draw, size):
    # Đầu mèo: mặt tròn, hai tai, mắt và mũi khoét trong suốt.
    center = size / 2
    unit = size / 100
    point = lambda x, y: (center + x * unit, center + y * unit)
    draw.polygon([point(-40, -8), point(-36, -44), point(-8, -26)], fill=255)
    draw.polygon([point(40, -8), point(36, -44), point(8, -26)], fill=255)
    draw.ellipse((center - 42 * unit, center - 30 * unit, center + 42 * unit, center + 36 * unit), fill=255)
    for x in (-17, 17):
        draw.ellipse((center + (x - 7) * unit, center - 7 * unit, center + (x + 7) * unit, center + 8 * unit), fill=0)
    draw.polygon([point(-5, 13), point(5, 13), point(0, 19)], fill=0)


def stamp_fish(draw, size):
    # Cá: thân bầu dục, đuôi tam giác, một mắt khoét trong suốt.
    center = size / 2
    unit = size / 100
    point = lambda x, y: (center + x * unit, center + y * unit)
    draw.ellipse((center - 44 * unit, center - 22 * unit, center + 22 * unit, center + 22 * unit), fill=255)
    draw.polygon([point(12, 0), point(44, -24), point(44, 24)], fill=255)
    draw.ellipse((center - 30 * unit, center - 10 * unit, center - 18 * unit, center + 2 * unit), fill=0)


def stamp_yarn(draw, size):
    # Cuộn len: hình tròn với các đường len khoét trong suốt.
    center = size / 2
    unit = size / 100
    draw.ellipse((center - 38 * unit, center - 38 * unit, center + 38 * unit, center + 38 * unit), fill=255)
    width = max(1, round(4 * unit))
    for offset in (-26, -6, 14):
        draw.arc((center + (offset - 42) * unit, center - 44 * unit, center + (offset + 22) * unit, center + 44 * unit), 300, 60, fill=0, width=width)
    for offset in (-10, 12):
        draw.arc((center - 40 * unit, center + (offset - 38) * unit, center + 40 * unit, center + (offset + 18) * unit), 30, 150, fill=0, width=width)


def game_pattern():
    # Tile nền màn chơi: hoạ tiết trắng trên nền trong suốt, nối liền khi lặp. Độ mờ chỉnh bằng màu SpriteRenderer trong Unity.
    size = 1024
    big = size * SUPERSAMPLE
    motif = 165 * SUPERSAMPLE   # cỡ mỗi hình; nhỏ hơn nhiều so với khoảng cách để nền thoáng, không lấn phần chơi
    mask = Image.new('L', (big, big), 0)
    placed = [
        (stamp_paw, 130, 120, -22), (stamp_cat, 470, 95, 10), (stamp_paw, 810, 135, 24),
        (stamp_fish, 300, 345, -18), (stamp_paw, 640, 360, 16), (stamp_yarn, 960, 390, 0),
        (stamp_cat, 120, 590, -12), (stamp_paw, 465, 620, -30), (stamp_fish, 805, 620, 20),
        (stamp_yarn, 300, 860, 0), (stamp_paw, 640, 880, 28), (stamp_cat, 950, 870, -8),
    ]
    for stamp, x, y, angle in placed:
        layer = Image.new('L', (motif, motif), 0)
        stamp(ImageDraw.Draw(layer), motif)
        layer = layer.rotate(angle, resample=Image.BICUBIC)
        left = x * SUPERSAMPLE - motif // 2
        top = y * SUPERSAMPLE - motif // 2
        # Hoạ tiết sát mép được vẽ lại ở phía đối diện để tile nối liền.
        for shift_x in (-big, 0, big):
            for shift_y in (-big, 0, big):
                mask.paste(255, (left + shift_x, top + shift_y), layer)
    mask = mask.resize((size, size), Image.LANCZOS)
    white = Image.new('L', (size, size), 255)
    Image.merge('RGBA', (white, white, white, mask)).save(OUT / 'GamePattern.png')
    print('ghi GamePattern')


if __name__ == '__main__':
    game_pattern()
    round_button()
    badge('BadgeCount', '#2a33a8', '#141a66', '#ffcf3f')
    badge('BadgePlus', '#7af03e', '#2fb30f', '#ffffff')
    framed_panel('Panel', (192, 192), 48, 10, '#6170ff', '#3b3fd6')
    framed_panel('PanelHeader', (192, 112), 36, 8, '#4b4be0', '#2c2ca8')
    framed_panel('LevelBadge', (160, 96), 28, 6, '#262d86', '#161b5c')
    framed_panel('TextBox', (128, 128), 30, 5, '#fff8ec', '#f6ead6', rim_top='#4a3aa8', rim_bottom='#2e2380', outline='#1c1450')
    button('ButtonGreen', '#9dff5a', '#2fcf00', '#1c8f00')
    button('ButtonBlue', '#7cc2ff', '#2f6fe8', '#1c3fa8')
    bar('BarBack', '#121a5a', '#1c2678', '#6f7dff')
    bar('BarFill', '#fff07a', '#ffa21a', '#b86b00')
    bubble()
    pill()
    rays()
    undo_icon()
    coin_icon()
    close_icon()
    for name, codepoint in (('IconMutate', 0x1F9EC), ('IconTransplant', 0x1F489), ('IconRevive', 0x2728), ('IconHint', 0x1F4A1),
                            ('IconSettings', 0x2699), ('IconHand', 0x1F446), ('IconSound', 0x1F50A), ('IconMute', 0x1F507),
                            ('IconMusic', 0x1F3B5), ('IconRestart', 0x1F504), ('IconPlay', 0x25B6)):
        emoji_icon(name, codepoint)
    star_empty(emoji_icon('IconStar', 0x2B50))
    (Path(__file__).resolve().parent / 'ui-slices.txt').write_text(''.join(f'{name} {" ".join(map(str, border))}\n' for name, border in SLICES.items()), encoding='utf-8')
    print('ghi tools/ui-slices.txt')
