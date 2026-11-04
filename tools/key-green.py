# Tách nền xanh lá của ảnh do Figma AI tạo thành PNG trong suốt, cắt sát hình rồi đưa về khung vuông.
#   python tools/key-green.py <ảnh vào> <ảnh ra> [cỡ ảnh ra, mặc định 512]
import sys

import numpy as np
from PIL import Image


def key_green(source, target, size=512):
    rgb = np.asarray(Image.open(source).convert('RGB'), dtype=np.float32) / 255.0
    red, green, blue = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    # Độ "xanh lá" so với hai kênh còn lại: nền thuần gần 1, hình gần 0, viền chuyển tiếp ở giữa.
    greenness = green - np.maximum(red, blue)
    alpha = 1.0 - np.clip((greenness - 0.12) / 0.38, 0.0, 1.0)
    # Bỏ ánh xanh còn dính ở viền.
    despilled = np.minimum(green, np.maximum(red, blue))
    output = np.dstack((red, np.where(alpha < 1.0, despilled, green), blue, alpha))
    image = Image.fromarray((output * 255.0 + 0.5).astype(np.uint8), 'RGBA')

    box = image.split()[3].point(lambda value: 255 if value > 16 else 0).getbbox()
    image = image.crop(box)
    side = max(image.size)
    square = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    square.paste(image, ((side - image.width) // 2, (side - image.height) // 2))
    square.resize((size, size), Image.LANCZOS).save(target)
    print('ghi', target, 'từ vùng', box)


if __name__ == '__main__':
    key_green(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 512)
