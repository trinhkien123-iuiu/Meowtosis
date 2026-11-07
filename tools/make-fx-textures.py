# Tạo texture hạt hiệu ứng tự vẽ cho các cơ chế (không dùng asset ripped).
#   python tools/make-fx-textures.py
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/Effects'
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


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    for name, make in (('ParticleBubble', bubble), ('ParticleGas', gas), ('ParticleGlint', glint)):
        make().save(OUT / f'{name}.png')
        print('saved', name)
