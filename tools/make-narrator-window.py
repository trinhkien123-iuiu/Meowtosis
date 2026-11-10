"""Khung "cửa sổ" tròn cho mèo dẫn chuyện cạnh bóng chat hướng dẫn, cùng kiểu ô avatar ở thanh trên:
lòng tối đỏ nâu, vành gỗ nâu có vệt sáng phía trên. Ba ảnh 256x256 vào unity/Assets/_Project/Art/UI/:
  NarratorWindowBack.png   lòng cửa sổ (tròn, tối dần ra mép)
  NarratorWindowFrame.png  vành gỗ (giữa trong suốt); mèo vẽ sau vành để hai chân trước gác lên mép cửa sổ
  python tools/make-narrator-window.py"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI'
SIZE = 256
SS = 4
S = SIZE * SS
INNER = 0.80          # bán kính lòng cửa sổ so với bán kính ngoài


def save(img, name):
    img.resize((SIZE, SIZE), Image.LANCZOS).save(OUT / name)
    print('ghi', name)


def disc(radius_frac, fill):
    img = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    r = S / 2 * radius_frac
    ImageDraw.Draw(img).ellipse((S / 2 - r, S / 2 - r, S / 2 + r, S / 2 + r), fill=fill)
    return img


def back():
    y, x = np.mgrid[0:S, 0:S].astype(np.float32)
    d = np.clip(np.hypot(x - S / 2, y - S * 0.42) / (S / 2 * INNER), 0, 1)[..., None]
    inside, edge = np.array((150, 44, 30), np.float32), np.array((70, 16, 10), np.float32)
    rgb = inside * (1 - d) + edge * d
    img = Image.fromarray(np.dstack([rgb, np.full((S, S), 255, np.float32)]).astype(np.uint8), 'RGBA')
    mask = disc(INNER + 0.02, (255, 255, 255, 255)).split()[3]
    out = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    out.paste(img, (0, 0), mask)
    return out


def frame():
    y = np.linspace(0, 1, S, dtype=np.float32)[:, None, None]
    light, dark = np.array((232, 162, 122), np.float32), np.array((140, 74, 52), np.float32)
    rgb = np.broadcast_to(light * (1 - y) + dark * y, (S, S, 3))
    wood = Image.fromarray(np.dstack([rgb, np.full((S, S), 255, np.float32)]).astype(np.uint8), 'RGBA')
    ring = disc(0.99, (255, 255, 255, 255)).split()[3]
    hole = disc(INNER, (255, 255, 255, 255)).split()[3]
    alpha = Image.fromarray(np.clip(np.array(ring, np.int32) - np.array(hole, np.int32), 0, 255).astype(np.uint8))
    out = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    out.paste(wood, (0, 0), alpha)
    d = ImageDraw.Draw(out)
    r_out, r_in = S / 2 * 0.99, S / 2 * INNER
    d.ellipse((S / 2 - r_out, S / 2 - r_out, S / 2 + r_out, S / 2 + r_out), outline=(96, 44, 28, 255), width=SS * 3)
    d.ellipse((S / 2 - r_in, S / 2 - r_in, S / 2 + r_in, S / 2 + r_in), outline=(96, 44, 28, 255), width=SS * 3)
    # vệt sáng cong phía trên vành và bóng trong mép lòng cửa sổ
    gloss = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    r_mid = (r_out + r_in) / 2
    ImageDraw.Draw(gloss).arc((S / 2 - r_mid, S / 2 - r_mid, S / 2 + r_mid, S / 2 + r_mid), 200, 330, fill=(255, 236, 214, 170), width=SS * 6)
    out = Image.alpha_composite(out, gloss.filter(ImageFilter.GaussianBlur(SS * 1.5)))
    shadow = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse((S / 2 - r_in, S / 2 - r_in, S / 2 + r_in, S / 2 + r_in), outline=(30, 6, 2, 150), width=SS * 7)
    shadow = shadow.filter(ImageFilter.GaussianBlur(SS * 3))
    shadow_alpha = Image.fromarray(np.minimum(np.array(shadow.split()[3]), np.array(hole)).astype(np.uint8))
    shadow.putalpha(shadow_alpha)
    return Image.alpha_composite(out, shadow)


if __name__ == '__main__':
    save(back(), 'NarratorWindowBack.png')
    save(frame(), 'NarratorWindowFrame.png')
