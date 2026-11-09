# Tạo các tiếng tổng hợp của game thành file WAV, đúng công thức Web Audio của prototype (prototype/meowtosis.html, sfx).
# Tiếng giao diện, thắng, thua, nhạc nền lấy từ Shikacat và tiếng mèo Mixkit nằm sẵn trong Audio/, không tạo ở đây.
# Ghi vào unity/Assets/_Project/Audio/Synth/. Cần Python 3 + numpy.
#   python tools/make-sfx.py
import wave
from pathlib import Path

import numpy as np

OUT = Path(__file__).resolve().parent.parent / 'unity/Assets/_Project/Audio/Synth'
RATE = 44100
RANDOM = np.random.default_rng(7)


def wave_shape(phase, kind):
    cycle = phase / (2 * np.pi) % 1.0
    if kind == 'sine':
        return np.sin(phase)
    if kind == 'square':
        return np.where(cycle < 0.5, 1.0, -1.0)
    if kind == 'sawtooth':
        return 2.0 * cycle - 1.0
    return 4.0 * np.abs(cycle - 0.5) - 1.0   # triangle


def tone(start_frequency, end_frequency, seconds, kind='sine', volume=0.16, delay=0.0):
    """Một nốt: tần số trượt từ start tới end, âm lượng lên trong 12 ms rồi tắt dần tới hết."""
    count = int(RATE * seconds)
    time = np.arange(count) / RATE
    frequency = start_frequency * (end_frequency / start_frequency) ** (time / seconds)
    phase = 2 * np.pi * np.cumsum(frequency) / RATE
    attack = 0.012
    envelope = np.where(time < attack,
                        0.0001 * (volume / 0.0001) ** (time / attack),
                        volume * (0.0001 / volume) ** ((time - attack) / max(seconds - attack, 0.001)))
    return delay, wave_shape(phase, kind) * envelope


def hiss(seconds, frequency=1500.0, q=0.8, volume=0.12, delay=0.0):
    """Tiếng ồn qua bộ lọc dải, tắt dần tuyến tính: xèo, phụt."""
    count = int(RATE * seconds)
    noise = (RANDOM.random(count) * 2 - 1) * (1 - np.arange(count) / count)
    omega = 2 * np.pi * frequency / RATE
    alpha = np.sin(omega) / (2 * q)
    b0, b2 = alpha, -alpha
    a0, a1, a2 = 1 + alpha, -2 * np.cos(omega), 1 - alpha
    out = np.zeros(count)
    for index in range(count):
        out[index] = ((b0 * noise[index] + (b2 * noise[index - 2] if index >= 2 else 0))
                      - (a1 * out[index - 1] if index >= 1 else 0) - (a2 * out[index - 2] if index >= 2 else 0)) / a0
    return delay, out * volume


def notes(frequencies, gap, kind='sine', volume=0.16):
    return [tone(f, f * 1.01, 0.2, kind, volume, index * gap) for index, f in enumerate(frequencies)]


def save(name, parts):
    length = max(int(RATE * delay) + len(samples) for delay, samples in parts)
    mix = np.zeros(length)
    for delay, samples in parts:
        start = int(RATE * delay)
        mix[start:start + len(samples)] += samples
    # Âm lượng trong prototype rất nhỏ vì trộn trực tiếp; nâng lên để file đủ to, chỉnh tiếp bằng volume trong GameSounds.
    mix = mix / max(np.abs(mix).max(), 0.001) * 0.9
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f'{name}.wav'), 'wb') as file:
        file.setnchannels(1)
        file.setsampwidth(2)
        file.setframerate(RATE)
        file.writeframes((mix * 32767).astype(np.int16).tobytes())
    print('ghi', name)


if __name__ == '__main__':
    save('Push', [tone(260, 190, 0.1, 'triangle', 0.1, 0.03)])
    save('Replace', [tone(340, 170, 0.14, 'triangle', 0.12)])
    save('Sparkle', [hiss(0.12, 3200, volume=0.06)])
    save('Toxic', [hiss(0.4, 5200, 0.6, 0.1), tone(230, 80, 0.32, 'sawtooth', 0.035)])
    save('Eat', [tone(190, 70, 0.15, 'square', 0.07), hiss(0.1, 700, volume=0.1)])
    save('Grumble', [tone(170, 80, 0.2, 'sawtooth', 0.05), tone(230, 120, 0.14, 'square', 0.03, 0.07), hiss(0.09, 900, volume=0.05)])
    save('Squish', [tone(430, 250, 0.09, 'sine', 0.09), tone(260, 520, 0.08, 'triangle', 0.05, 0.06)])
    save('Booster', notes([784, 988, 1175, 1568], 0.05, 'sine', 0.1) + [hiss(0.45, 6500, volume=0.03)])
    save('Revive', notes([523, 659, 784, 1047, 1319], 0.07, 'triangle', 0.12))
    save('Coin', [tone(1568, 1584, 0.07, 'sine', 0.07), tone(2352, 2367, 0.1, 'sine', 0.045, 0.035)])
    save('Cashout', notes([1047, 1319, 1568, 2093], 0.055, 'triangle', 0.1) + [hiss(0.3, 7500, volume=0.045, delay=0.1)])
    # Chọn level: tiếng "bộp" đi lên rồi 3 nốt vui, phải ngắn hơn hiệu ứng chọn (0,38 giây) vì sang scene mới là tắt.
    save('LevelSelect', [tone(330, 660, 0.09, 'sine', 0.13), tone(784, 792, 0.14, 'triangle', 0.1, 0.05),
                         tone(1047, 1058, 0.16, 'triangle', 0.1, 0.1), tone(1319, 1333, 0.2, 'sine', 0.09, 0.15)])
    # Qua cổng dịch chuyển: tiếng hút vút xuống rồi bật ra vút lên, kèm hơi gió.
    save('Portal', [tone(900, 160, 0.16, 'sine', 0.1), hiss(0.22, 1800, 1.2, 0.07),
                    tone(220, 1040, 0.2, 'triangle', 0.1, 0.12), tone(1568, 1580, 0.16, 'sine', 0.05, 0.24)])
    # Mèo hai màu đổi màu: hai nốt "tính tính" đi lên rồi lướt, như tắc kè đổi sắc.
    save('ColorSwitch', [tone(660, 680, 0.1, 'triangle', 0.11), tone(880, 900, 0.1, 'triangle', 0.11, 0.07),
                         tone(1320, 1980, 0.18, 'sine', 0.09, 0.14), hiss(0.2, 6000, volume=0.03, delay=0.1)])
    # Booster Hoán đổi: tụ phép (vút lên lấp lánh, 0–0,45 giây) rồi "bùm" đổi màu ở 0,45 giây kèm chuỗi chuông đi lên.
    save('BoosterMutate', [tone(300, 1500, 0.45, 'sine', 0.07), tone(450, 2250, 0.45, 'triangle', 0.04), hiss(0.45, 5200, 1.0, 0.04),
                           tone(180, 70, 0.22, 'sine', 0.16, 0.45), hiss(0.18, 900, 0.8, 0.1, 0.45)]
                          + [tone(f, f * 1.01, 0.22, 'triangle', 0.09, 0.47 + index * 0.055) for index, f in enumerate([1047, 1319, 1568, 2093])]
                          + [hiss(0.4, 7800, volume=0.035, delay=0.5)])
    # Booster Nhân bản: "vút" bản sao bay (0–0,5 giây) rồi "bóc" mọc ra ở 0,5 giây và tiếng chuông kép.
    save('BoosterClone', [hiss(0.5, 1400, 1.4, 0.07), tone(380, 900, 0.5, 'sine', 0.05),
                          tone(520, 1100, 0.08, 'sine', 0.14, 0.5), tone(1319, 1330, 0.18, 'triangle', 0.08, 0.55),
                          tone(1760, 1775, 0.24, 'triangle', 0.07, 0.62), hiss(0.25, 7000, volume=0.03, delay=0.55)])
    # Mất một sao: tiếng "rắc" vỡ kính (nhiễu cao ngắn) rồi hai nốt buồn đi xuống, kèm tiếng lách tách mảnh rơi.
    save('StarLost', [hiss(0.07, 5200, 2.5, 0.16), tone(1568, 1500, 0.05, 'square', 0.05),
                      tone(523, 392, 0.2, 'triangle', 0.12, 0.06), tone(392, 262, 0.28, 'triangle', 0.11, 0.2),
                      hiss(0.05, 3600, 2.5, 0.07, 0.26), hiss(0.05, 4200, 2.5, 0.06, 0.34), hiss(0.04, 3000, 2.5, 0.05, 0.42)])
    # Khử 3 (nước dọn chỗ, trung tính): ba tiếng "bụp" nhỏ hạ dần (ba con mèo tan đi) rồi một tiếng "phụt" khói, không có giai điệu đi lên
    # để không nghe như phần thưởng.
    save('ClearPoof', [tone(560, 260, 0.09, 'sine', 0.15), hiss(0.05, 1900, 0.8, 0.06),
                       tone(480, 220, 0.09, 'sine', 0.14, 0.075), hiss(0.05, 1700, 0.8, 0.05, 0.075),
                       tone(400, 190, 0.1, 'sine', 0.13, 0.15), hiss(0.06, 1500, 0.8, 0.05, 0.15),
                       hiss(0.2, 650, 0.7, 0.07, 0.2)])
    # Bấm level chưa mở: hai nhịp trầm đi xuống kèm tiếng lách cách của ổ khoá, báo chưa được nhưng không gắt.
    save('LevelLocked', [tone(233, 165, 0.1, 'triangle', 0.14), hiss(0.03, 3400, 2.0, 0.07),
                         tone(175, 117, 0.13, 'triangle', 0.13, 0.1), hiss(0.03, 2800, 2.0, 0.06, 0.1)])
