"""Lấy asset UI từ dự án Bounce_Blast của chính chủ (cá nhân, phi thương mại) vào Art/UI/Bounce.

Sprite của Bounce_Blast nằm trong atlas: đọc vùng cắt (textureRect) từ file .asset rồi cắt từ ảnh atlas.
Ảnh 9-slice chỉ lưu nửa (hậu tố _mir) thì ghép đôi. Viền 9-slice ghi vào file .meta (Unity đọc khi nhập).
Chạy: python tools/import-bounce-ui.py (chạy lần hai sau khi Unity đã nhập ảnh để viền được ghi vào .meta).
"""
import re
from pathlib import Path

from PIL import Image

SOURCE = Path('D:/GitHub/Bounce_Blast/Assets/_Project/Art/UI')
OUTPUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Art/UI/Bounce'
ATLAS_ROOT = Path('D:/GitHub/Bounce_Blast/Assets')


def atlas_by_guid():
    result = {}
    for meta in ATLAS_ROOT.rglob('sactx-*.png.meta'):
        found = re.search(r'guid: (\w+)', meta.read_text(errors='ignore'))
        if found:
            result[found.group(1)] = meta.with_suffix('')
    return result


ATLASES = atlas_by_guid()


def cut(folder, name):
    """Cắt một sprite ra khỏi atlas của nó."""
    text = (SOURCE / folder / 'Sprites' / (name + '.asset')).read_text(errors='ignore')
    guid = re.search(r'texture: \{fileID: 2800000, guid: (\w+)', text).group(1)
    x, y, width, height = [float(v) for v in re.search(
        r'textureRect:\s+serializedVersion: 2\s+x: ([\-\d.e]+)\s+y: ([\-\d.e]+)\s+width: ([\-\d.e]+)\s+height: ([\-\d.e]+)', text).groups()]
    atlas = Image.open(ATLASES[guid]).convert('RGBA')
    return atlas.crop((round(x), round(atlas.height - y - height), round(x + width), round(atlas.height - y)))


def mirror_pair(half):
    full = Image.new('RGBA', (half.width * 2, half.height))
    full.alpha_composite(half, (0, 0))
    full.alpha_composite(half.transpose(Image.FLIP_LEFT_RIGHT), (half.width, 0))
    return full


def write_border(name, left, bottom, right, top):
    meta = OUTPUT / (name + '.png.meta')
    if not meta.exists():
        return False
    text = meta.read_text(encoding='utf-8')
    text = re.sub(r'spriteBorder: \{[^}]*\}', f'spriteBorder: {{x: {left}, y: {bottom}, z: {right}, w: {top}}}', text)
    meta.write_text(text, encoding='utf-8')
    return True


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    pending = {}

    def save(name, image, border=None, scale=1.0):
        if scale != 1.0:
            image = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
        image.save(OUTPUT / (name + '.png'))
        if border:
            pending[name] = [round(v * scale) for v in border]
        print('ghi', name, image.size)

    save('BounceSettings', cut('Hud', 'Settings'), scale=0.5)
    save('BounceLevel', cut('Hud', 'Level'), scale=0.5)
    save('BounceHeart', cut('Popup', 'Heart'), scale=0.5)
    save('BounceClose', cut('Popup', 'Close'))
    save('BounceBadge', cut('Booster', 'Number'), scale=0.25)
    save('BounceBadgeOrange', cut('Booster', 'Orange Number'), scale=0.25)
    save('BounceButtonGreen', mirror_pair(cut('Common', 'Play Button')), border=(162, 174, 162, 167), scale=0.4)

    save('BounceTabHome', cut('Menu', 'BigHome'), scale=0.25)
    save('BounceTabShop', cut('Menu', 'BigShop'), scale=0.25)
    save('BounceTabSettings', cut('Menu', 'BigSettings'))
    save('BounceTabBar', cut('Menu', 'Menu'))
    save('BounceBarLine', cut('Menu', 'HomeButtomSeperator'))
    save('BouncePageBackground', cut('Menu', 'Coming Soon'))
    save('BounceTitleTab', cut('Menu', 'ShopBG'))
    save('BounceTopBar', cut('Menu', 'TopShopBar'))
    save('BounceRow', cut('Menu', 'Setting Home Buttons'), border=(79, 59, 79, 59))
    save('BounceToggleOn', mirror_pair(cut('Popup', 'On Button_mir')), scale=0.5)
    save('BounceToggleOff', mirror_pair(cut('Popup', 'Off Button_mir')), scale=0.5)
    save('BounceTabSelected', cut('Menu', 'Selected Menu'))
    save('BouncePlayButton', cut('Menu', 'Play Button 1'), border=(166, 138, 167, 151), scale=0.6)
    # Viền ôm trọn hai đầu tròn: ảnh cao 256 nên mỗi đầu rộng 128, chỉ kéo giãn phần thân thẳng ở giữa.
    save('BouncePill', cut('Menu', 'Heart-Coin BG'), border=(128, 126, 128, 126), scale=0.5)
    save('BounceBack', cut('Popup', 'Sprite - Arrow Back 2'))
    save('BounceAvatarFrame', mirror_pair(cut('Menu', 'Selected Profile Frame_mir')), scale=0.5)
    save('BounceSoundOn', cut('Popup', 'Volume on'), scale=0.5)
    save('BounceSoundOff', cut('Popup', 'Volume off'), scale=0.5)
    save('BounceMusicOn', cut('Popup', 'Music on'), scale=0.5)
    save('BounceMusicOff', cut('Popup', 'Music off'), scale=0.5)

    popup = SOURCE / 'Popup/Textures'
    save('BouncePanel', Image.open(popup / '9SliceBlueFrame.png').convert('RGBA'), border=(296, 300, 295, 300), scale=0.5)
    save('BounceButtonOrange', Image.open(popup / 'Give Up Button.png').convert('RGBA'), border=(150, 150, 150, 150), scale=0.4)

    for name, border in pending.items():
        if write_border(name, *border):
            print('viền', name, border)


if __name__ == '__main__':
    main()
