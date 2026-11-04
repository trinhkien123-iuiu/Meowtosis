# Vẽ hình tạm cho những thứ Shikacat không có (vi khuẩn, virus, ô độc, màng, bịt mắt ngủ, chữ zz, bóng đổ, ô bo góc),
# theo đúng toạ độ và màu của prototype (prototype/meowtosis.html: cellShape, face, sleepMask, baseSvg, wallsSvg).
# Ghi vào unity/Assets/_Project/Art/Placeholder/. Cần Python 3 + Pillow + numpy.
#   python tools/make-placeholder-art.py
#
# Toạ độ tính như prototype: một ô rộng 90, tâm ô m = 45. Mỗi hình vẽ trên khung rộng 1,5 ô, ô nằm giữa khung,
# để phần lòi ra ngoài ô (chữ zz, gai virus, bóng đổ) không bị cắt. Trong Unity hiển thị mọi hình này rộng 1,5 ô.
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/Placeholder'
CELL = 90
FRAME = CELL * 1.5           # khung vẽ: 1,5 ô
OFFSET = (FRAME - CELL) / 2  # ô nằm giữa khung
PIXELS = 540                 # cỡ ảnh xuất ra
SUPERSAMPLE = 2              # vẽ to gấp đôi rồi thu nhỏ cho mịn
M = CELL / 2
INK = '#2b0f2e'


class Drawing:
    def __init__(self, frame=FRAME, offset=OFFSET):
        self.scale = PIXELS * SUPERSAMPLE / frame
        self.offset = offset
        size = PIXELS * SUPERSAMPLE
        self.image = Image.new('RGBA', (size, size), (0, 0, 0, 0))

    def point(self, x, y):
        return ((x + self.offset) * self.scale, (y + self.offset) * self.scale)

    def length(self, value):
        return value * self.scale

    def layer(self):
        return Image.new('RGBA', self.image.size, (0, 0, 0, 0))

    def paste(self, layer):
        self.image = Image.alpha_composite(self.image, layer)

    def save(self, name):
        OUT.mkdir(parents=True, exist_ok=True)
        self.image.resize((PIXELS, PIXELS), Image.LANCZOS).save(OUT / f'{name}.png')
        print('ghi', name)


def rgba(color, opacity=1.0):
    color = color.lstrip('#')
    return tuple(int(color[i:i + 2], 16) for i in (0, 2, 4)) + (round(255 * opacity),)


def radial_fill(drawing, mask, box, stops, center=(0.5, 0.5), radius=0.5):
    """Tô gradient tròn (như radialGradient của SVG, đơn vị theo khung bao `box`) vào vùng `mask`."""
    left, top, right, bottom = box
    height, width = mask.size[1], mask.size[0]
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32)
    cx = left + (right - left) * center[0]
    cy = top + (bottom - top) * center[1]
    dx = (xs - cx) / ((right - left) * radius)
    dy = (ys - cy) / ((bottom - top) * radius)
    t = np.clip(np.sqrt(dx * dx + dy * dy), 0, 1)
    offsets = [offset for offset, _ in stops]
    colors = np.array([rgba(color)[:3] for _, color in stops], dtype=np.float32)
    channels = [np.interp(t, offsets, colors[:, channel]) for channel in range(3)]
    alpha = np.array(mask, dtype=np.float32)
    pixels = np.dstack(channels + [alpha]).astype(np.uint8)
    drawing.paste(Image.fromarray(pixels, 'RGBA'))


def linear_fill(drawing, mask, box, top_color, bottom_color, diagonal=False):
    left, top, right, bottom = box
    height, width = mask.size[1], mask.size[0]
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32)
    t = (ys - top) / max(bottom - top, 1)
    if diagonal:
        t = ((xs - left) / max(right - left, 1) + t) / 2
    t = np.clip(t, 0, 1)
    start, end = np.array(rgba(top_color)[:3], np.float32), np.array(rgba(bottom_color)[:3], np.float32)
    channels = [start[c] + (end[c] - start[c]) * t for c in range(3)]
    pixels = np.dstack(channels + [np.array(mask, np.float32)]).astype(np.uint8)
    drawing.paste(Image.fromarray(pixels, 'RGBA'))


def shape_mask(drawing, draw_shape):
    mask = Image.new('L', drawing.image.size, 0)
    draw_shape(ImageDraw.Draw(mask))
    return mask


def quadratic(start, control, end, steps=24):
    return [((1 - t) ** 2 * start[0] + 2 * (1 - t) * t * control[0] + t ** 2 * end[0],
             (1 - t) ** 2 * start[1] + 2 * (1 - t) * t * control[1] + t ** 2 * end[1])
            for t in (i / steps for i in range(steps + 1))]


def stroke_line(drawing, points, color, width, opacity=1.0):
    """Đường gấp khúc nét tròn (đầu và chỗ nối bo tròn)."""
    layer = drawing.layer()
    draw = ImageDraw.Draw(layer)
    pixels = [drawing.point(x, y) for x, y in points]
    thickness = drawing.length(width)
    draw.line(pixels, fill=rgba(color, opacity), width=round(thickness), joint='curve')
    for x, y in (pixels[0], pixels[-1]):
        r = thickness / 2
        draw.ellipse((x - r, y - r, x + r, y + r), fill=rgba(color, opacity))
    drawing.paste(layer)


def ellipse(drawing, cx, cy, rx, ry, color, opacity=1.0, angle=0.0):
    layer = drawing.layer()
    x, y = drawing.point(cx, cy)
    w, h = drawing.length(rx), drawing.length(ry)
    if angle == 0:
        ImageDraw.Draw(layer).ellipse((x - w, y - h, x + w, y + h), fill=rgba(color, opacity))
    else:
        points = [(x + w * math.cos(t) * math.cos(math.radians(angle)) - h * math.sin(t) * math.sin(math.radians(angle)),
                   y + w * math.cos(t) * math.sin(math.radians(angle)) + h * math.sin(t) * math.cos(math.radians(angle)))
                  for t in (i * math.pi / 36 for i in range(72))]
        ImageDraw.Draw(layer).polygon(points, fill=rgba(color, opacity))
    drawing.paste(layer)


def outline_round_rect(drawing, x, y, w, h, r, color, width):
    layer = drawing.layer()
    box = (*drawing.point(x, y), *drawing.point(x + w, y + h))
    ImageDraw.Draw(layer).rounded_rectangle(box, radius=drawing.length(r), outline=rgba(color), width=round(drawing.length(width)))
    drawing.paste(layer)


def outline_polygon(drawing, points, color, width):
    stroke_line(drawing, points + [points[0], points[1]], color, width)


def eyes(drawing, look=(0, 0)):
    lx, ly = look[0] * 3, look[1] * 3
    for side in (-11, 11):
        ellipse(drawing, M + side, M - 4, 7.5, 8.5, '#ffffff')
        ellipse(drawing, M + side + lx, M - 3 + ly, 4.3, 4.3, INK)
    ellipse(drawing, M - 12.5 + lx, M - 5 + ly, 1.5, 1.5, '#ffffff')
    ellipse(drawing, M + 9.5 + lx, M - 5 + ly, 1.5, 1.5, '#ffffff')


def shine(drawing, dx, dy, rx, ry, angle):
    ellipse(drawing, M + dx, M + dy, rx, ry, '#ffffff', 0.6, angle)


def germ():
    drawing = Drawing()
    dark = '#2f7a14'
    stroke_line(drawing, quadratic((M - 31, M + 8), (M - 43, M + 10), (M - 44, M + 23)), dark, 3.5)
    stroke_line(drawing, quadratic((M + 31, M - 8), (M + 43, M - 10), (M + 44, M - 23)), dark, 3.5)
    body = (*drawing.point(M - 34, M - 22), *drawing.point(M + 34, M + 22))
    mask = shape_mask(drawing, lambda d: d.rounded_rectangle(body, radius=drawing.length(22), fill=255))
    radial_fill(drawing, mask, body, [(0, '#e2ffc4'), (0.45, '#84db4c'), (1, '#3f9a1e')], (0.35, 0.3), 0.8)
    outline_round_rect(drawing, M - 34, M - 22, 68, 44, 22, dark, 3)
    shine(drawing, -15, -11, 10, 4.5, -18)
    eyes(drawing)
    stroke_line(drawing, quadratic((M - 7, M + 10), (M, M + 14), (M + 6, M + 7)), INK, 3)
    drawing.save('Germ')


def virus(name, look):
    drawing = Drawing()
    spikes = [(M + (40 if k % 2 == 0 else 30) * math.cos(math.pi * k / 12),
               M + (40 if k % 2 == 0 else 30) * math.sin(math.pi * k / 12)) for k in range(24)]
    pixels = [drawing.point(x, y) for x, y in spikes]
    box = (*drawing.point(M - 40, M - 40), *drawing.point(M + 40, M + 40))
    mask = shape_mask(drawing, lambda d: d.polygon(pixels, fill=255))
    radial_fill(drawing, mask, box, [(0, '#f1deff'), (0.45, '#b56cff'), (1, '#6b2fb5')], (0.35, 0.3), 0.8)
    outline_polygon(drawing, spikes, '#55208f', 3)
    shine(drawing, -13, -20, 11, 5.5, -32)
    eyes(drawing, look)
    stroke_line(drawing, [(M - 19, M - 16), (M - 9, M - 11)], INK, 3.5)
    stroke_line(drawing, [(M + 19, M - 16), (M + 9, M - 11)], INK, 3.5)
    stroke_line(drawing, [(M - 9, M + 13), (M - 6, M + 9), (M - 3, M + 13), (M, M + 9), (M + 3, M + 13), (M + 6, M + 9)], INK, 2.5)
    drawing.save(name)


def toxic_tile():
    drawing = Drawing()
    box = (*drawing.point(2, 2), *drawing.point(88, 88))
    mask = shape_mask(drawing, lambda d: d.rounded_rectangle(box, radius=drawing.length(22), fill=255))
    radial_fill(drawing, mask, box, [(0, '#ecffa6'), (0.6, '#a6dc3f'), (1, '#6c9a1a')], (0.5, 0.45), 0.72)
    outline_round_rect(drawing, 2, 2, 86, 86, 22, '#5f8a14', 2.5)
    for x, y, r in ((22, 64, 6), (64, 70, 8), (46, 60, 5), (70, 40, 4)):
        ellipse(drawing, x, y, r, r, '#f4ffc9')
    drawing.save('ToxicTile')


def sleep_mask():
    drawing = Drawing()
    box = (*drawing.point(M - 30, M - 27), *drawing.point(M + 26, M - 11))
    mask = shape_mask(drawing, lambda d: d.rounded_rectangle(box, radius=drawing.length(8), fill=255))
    radial_fill(drawing, mask, box, [(0, '#26357f'), (1, '#26357f')])
    outline_round_rect(drawing, M - 30, M - 27, 56, 16, 8, '#ffffff', 2)
    stroke_line(drawing, quadratic((M - 20, M - 21), (M - 15, M - 16), (M - 10, M - 21)), '#ffffff', 2.5)
    stroke_line(drawing, quadratic((M + 5, M - 21), (M + 10, M - 16), (M + 15, M - 21)), '#ffffff', 2.5)
    drawing.save('SleepMask')


def sleep_letters():
    drawing = Drawing()
    layer = drawing.layer()
    draw = ImageDraw.Draw(layer)
    for x, y, size in ((CELL - 30, 22, 28), (CELL - 14, 6, 19)):
        font = load_font(round(drawing.length(size)))
        left, baseline = drawing.point(x, y)
        draw.text((left, baseline), 'z', font=font, anchor='ls', fill=rgba('#ffffff'),
                  stroke_width=round(drawing.length(2.5)), stroke_fill=rgba('#1a2f8f'))
    drawing.paste(layer)
    drawing.save('SleepLetters')


def load_font(size):
    for path in ('C:/Windows/Fonts/arialbd.ttf', '/System/Library/Fonts/Supplemental/Arial Bold.ttf',
                 '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def shadow():
    drawing = Drawing()
    ellipse(drawing, M, M + 38, 25, 5, '#0a1450', 0.25)
    drawing.save('Shadow')


def membrane():
    # Dải thạch dọc giữa hai ô (cạnh trái/phải); Unity xoay 90° cho cạnh trên/dưới.
    drawing = Drawing()
    box = (*drawing.point(M - 6, M - 38), *drawing.point(M + 6, M + 38))
    mask = shape_mask(drawing, lambda d: d.rounded_rectangle(box, radius=drawing.length(6), fill=255))
    linear_fill(drawing, mask, box, '#7ff0de', '#1fa894', diagonal=True)
    outline_round_rect(drawing, M - 6, M - 38, 12, 76, 6, '#0f7062', 2.5)
    drawing.save('MembraneBar')

    # Nút tròn có mũi tên chỉ sang phải (hướng được đi qua); Unity xoay theo hướng.
    drawing = Drawing()
    ellipse(drawing, M, M, 16.25, 16.25, '#ffffff')
    ellipse(drawing, M, M, 13.75, 13.75, '#0f7062')
    stroke_line(drawing, [(M - 5, M - 8), (M + 4, M), (M - 5, M + 8)], '#ffffff', 4)
    drawing.save('MembraneArrow')


def rounded_square():
    # Ô vuông trắng bo góc như Tile_Board, Unity tô màu để làm khung gỗ và lòng khay của bàn.
    drawing = Drawing(frame=CELL, offset=0)
    box = (*drawing.point(0, 0), *drawing.point(CELL, CELL))
    layer = drawing.layer()
    ImageDraw.Draw(layer).rounded_rectangle(box, radius=drawing.length(24), fill=rgba('#ffffff'))
    drawing.paste(layer)
    drawing.save('RoundedSquare')


def hint_arrow():
    # Mũi tên gợi ý chỉ sang phải, xuất phát từ tâm ô (như arrowSvg của prototype); Unity xoay theo hướng.
    # Đầu mũi tên dài quá khung 1,5 ô nên dùng khung rộng hơn, tâm ô vẫn ở giữa ảnh (scale prefab HintArrow tính theo khung này).
    frame = CELL * 2.2
    drawing = Drawing(frame, (frame - CELL) / 2)
    # Vẽ toàn bộ viền trước rồi phủ phần trắng lên, để thân và đầu dùng chung một đường viền cùng độ dày, không có vạch ở chỗ nối.
    outline = 4
    shaft = [(M + 12, M), (M + 56, M)]
    head = [(M + 52, M - 15), (M + 76, M), (M + 52, M + 15)]
    stroke_line(drawing, shaft, '#10206b', 9 + outline * 2)
    for start, end in zip(head, head[1:] + head[:1]):
        stroke_line(drawing, [start, end], '#10206b', outline * 2)
    stroke_line(drawing, shaft, '#ffffff', 9)
    layer = drawing.layer()
    ImageDraw.Draw(layer).polygon([drawing.point(x, y) for x, y in head], fill=rgba('#ffffff'))
    drawing.paste(layer)
    drawing.save('HintArrow')


def cell_highlight():
    # Viền vàng sáng quanh ô đang chọn (booster).
    drawing = Drawing()
    outline_round_rect(drawing, 1, 1, 88, 88, 24, '#ffe066', 6)
    outline_round_rect(drawing, -2, -2, 94, 94, 27, '#ffffff', 2)
    drawing.save('CellHighlight')


def square():
    # Ô vuông trắng không bo góc: dải nối giữa hai ô của khung bàn (Unity tô màu và kéo dãn).
    OUT.mkdir(parents=True, exist_ok=True)
    Image.new('RGBA', (64, 64), (255, 255, 255, 255)).save(OUT / 'Square.png')
    print('ghi Square')


def face_overlays():
    # Vẽ đè lên mặt mèo Shikacat theo từng màu (ảnh không tách mắt, miệng): phủ mắt/miệng bằng màu lông rồi vẽ lên.
    # Vị trí đo trên ảnh mèo như prototype (faceOf, shutEyes, sadFace).
    fur_colors = {'Red': ('#fd8265', '#9c0f0f'), 'Blue': ('#388bde', '#123f8f'), 'Yellow': ('#f9da83', '#7a3104'),
                  'Pink': ('#f5a7d2', '#5d031c'), 'Purple': ('#886dce', '#2b0a63')}
    left_eye, right_eye, eye_y, mouth_x, mouth_y, half_width = M - 15, M + 10.7, M - 18.3, M - 2.3, M - 12.6, 6

    def cover_eyes(drawing, fur):
        for x in (left_eye, right_eye):
            ellipse(drawing, x, eye_y, 8.2, 7.6, fur)

    def cover_mouth(drawing, fur):
        ellipse(drawing, mouth_x, mouth_y + 0.4, 4.6, 2.4, fur)

    def shut_eyes(drawing, ink, bend):
        for x in (left_eye, right_eye):
            stroke_line(drawing, quadratic((x - half_width, eye_y + 1), (x, eye_y + 1 + bend), (x + half_width, eye_y + 1)), ink, 2.6)

    for name, (fur, ink) in fur_colors.items():
        drawing = Drawing()
        cover_eyes(drawing, fur)
        shut_eyes(drawing, ink, 4)
        drawing.save(f'FaceBlink{name}')

        drawing = Drawing()
        cover_eyes(drawing, fur)
        cover_mouth(drawing, fur)
        shut_eyes(drawing, ink, -7)
        stroke_line(drawing, quadratic((mouth_x - 3.5, mouth_y + 1.5), (mouth_x, mouth_y - 1.9), (mouth_x + 3.5, mouth_y + 1.5)), ink, 2.2)
        drawing.save(f'FaceSad{name}')

        drawing = Drawing()
        cover_eyes(drawing, fur)
        cover_mouth(drawing, fur)
        shut_eyes(drawing, ink, 4)
        ellipse(drawing, mouth_x, mouth_y + 2, 4.6, 5.6, ink)
        ellipse(drawing, mouth_x, mouth_y + 3.6, 3, 2.6, '#ff8fb5')
        drawing.save(f'FaceYawn{name}')


def effect_sprites():
    # Mắt @@ (một mắt, xoay trong Unity), giọt mồ hôi, vầng hào quang của hồn mèo, hàm răng virus, sao bay vòng.
    drawing = Drawing(frame=22, offset=11)
    ellipse(drawing, 0, 0, 9, 9, '#ffffff')
    layer = drawing.layer()
    x, y = drawing.point(0, 0)
    ImageDraw.Draw(layer).text((x, y), '@', font=load_font(round(drawing.length(16))), anchor='mm', fill=rgba(INK))
    drawing.paste(layer)
    drawing.save('SwirlEye')

    drawing = Drawing(frame=26, offset=13)
    top, right, bottom = (0, -8), (6, 4), (0, 10)
    shape = quadratic(top, (5, -2), right) + quadratic(right, (6, 10), bottom) + quadratic(bottom, (-6, 10), (-6, 4)) + quadratic((-6, 4), (-5, -2), top)
    layer = drawing.layer()
    ImageDraw.Draw(layer).polygon([drawing.point(px, py) for px, py in shape], fill=rgba('#8fd3ff'))
    drawing.paste(layer)
    stroke_line(drawing, shape + [shape[0]], '#2d7fd1', 1.6)
    ellipse(drawing, -2, 2.5, 1.5, 2.4, '#ffffff', 0.85)
    drawing.save('SweatDrop')

    drawing = Drawing()
    ring = [(M + 17 * math.cos(a * math.pi / 24), M - 47 + 5 * math.sin(a * math.pi / 24)) for a in range(49)]
    stroke_line(drawing, ring, '#fff6a0', 3.5)
    drawing.save('Halo')

    drawing = Drawing()
    half_disc = [(M + 39 * math.cos(math.pi + a * math.pi / 40), M + 39 * math.sin(math.pi + a * math.pi / 40)) for a in range(41)]
    layer = drawing.layer()
    ImageDraw.Draw(layer).polygon([drawing.point(px, py) for px, py in half_disc], fill=rgba('#b56cff'))
    drawing.paste(layer)
    stroke_line(drawing, half_disc + [half_disc[0]], '#55208f', 3)
    teeth = [(14, 45)]
    for k in range(6):
        teeth += [(14 + k * 11 + 5.5, 56), (14 + (k + 1) * 11, 45)]
    layer = drawing.layer()
    ImageDraw.Draw(layer).polygon([drawing.point(px, py) for px, py in teeth], fill=rgba('#ffffff'))
    drawing.paste(layer)
    stroke_line(drawing, teeth + [teeth[0]], '#55208f', 1.5)
    drawing.save('Jaw')

    drawing = Drawing(frame=20, offset=10)
    star = []
    for k in range(10):
        radius = 8 if k % 2 == 0 else 3.2
        star.append((radius * math.cos(-math.pi / 2 + k * math.pi / 5), radius * math.sin(-math.pi / 2 + k * math.pi / 5)))
    layer = drawing.layer()
    ImageDraw.Draw(layer).polygon([drawing.point(px, py) for px, py in star], fill=rgba('#ffe27a'))
    drawing.paste(layer)
    stroke_line(drawing, star + [star[0]], '#b88300', 1.5)
    drawing.save('OrbitStar')


def particle_sprites():
    # Hạt trắng cho Particle System (Unity tô màu): chấm tròn, sao 4 cánh, khói mờ, vòng tròn, ô vuông confetti.
    size = 128
    big = size * 4
    OUT.mkdir(parents=True, exist_ok=True)

    def save(name, mask):
        image = Image.new('RGBA', (size, size), (255, 255, 255, 255))
        image.putalpha(mask.resize((size, size), Image.LANCZOS))
        image.save(OUT / f'{name}.png')
        print('ghi', name)

    dot = Image.new('L', (big, big), 0)
    ImageDraw.Draw(dot).ellipse((8, 8, big - 8, big - 8), fill=255)
    save('ParticleDot', dot)

    star = Image.new('L', (big, big), 0)
    center = big / 2
    points = []
    for k in range(8):
        radius = (center - 4) if k % 2 == 0 else (center - 4) * 0.35
        points.append((center + radius * math.cos(k * math.pi / 4), center + radius * math.sin(k * math.pi / 4)))
    ImageDraw.Draw(star).polygon(points, fill=255)
    save('ParticleStar', star)

    puff = Image.new('L', (big, big), 0)
    ImageDraw.Draw(puff).ellipse((big * 0.2, big * 0.2, big * 0.8, big * 0.8), fill=255)
    save('ParticlePuff', puff.filter(ImageFilter.GaussianBlur(big / 12)))

    ring = Image.new('L', (big, big), 0)
    draw = ImageDraw.Draw(ring)
    draw.ellipse((8, 8, big - 8, big - 8), fill=255)
    draw.ellipse((big * 0.14, big * 0.14, big * 0.86, big * 0.86), fill=0)
    save('ParticleRing', ring)

    save('ParticleSquare', Image.new('L', (big, big), 255))


def soul_cat():
    # Hồn mèo: ảnh mèo Shikacat chuyển trắng sáng, giống filter brightness + saturate(0) của prototype. Dùng chung cho mọi màu.
    source = Image.open(Path(__file__).resolve().parent.parent / 'prototype/shikacat/cats/Image_Cat_Red.png').convert('RGBA')
    pixels = np.array(source, np.float32)
    gray = np.clip(pixels[..., :3].mean(axis=2) * 1.9, 0, 255)
    pixels[..., 0] = pixels[..., 1] = pixels[..., 2] = gray
    pixels[..., 3] *= 0.72
    Image.fromarray(pixels.astype(np.uint8), 'RGBA').save(OUT / 'SoulCat.png')
    print('ghi SoulCat')


if __name__ == '__main__':
    germ()
    for direction, look in (('Up', (0, -1)), ('Down', (0, 1)), ('Left', (-1, 0)), ('Right', (1, 0))):
        virus(f'Virus{direction}', look)
    toxic_tile()
    sleep_mask()
    sleep_letters()
    shadow()
    membrane()
    rounded_square()
    square()
    hint_arrow()
    cell_highlight()
    face_overlays()
    effect_sprites()
    particle_sprites()
    soul_cat()
