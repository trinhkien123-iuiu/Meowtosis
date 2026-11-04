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
    # Bấm level chưa mở: hai nhịp trầm đi xuống kèm tiếng lách cách của ổ khoá, báo chưa được nhưng không gắt.
    save('LevelLocked', [tone(233, 165, 0.1, 'triangle', 0.14), hiss(0.03, 3400, 2.0, 0.07),
                         tone(175, 117, 0.13, 'triangle', 0.13, 0.1), hiss(0.03, 2800, 2.0, 0.06, 0.1)])
