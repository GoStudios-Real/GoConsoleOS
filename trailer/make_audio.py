"""Synthesize the GoConsoleOS trailer soundtrack (45s, 44.1kHz stereo)."""
import wave, math, os
import numpy as np

SR = 44100
DUR = 45.0
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.join(os.path.dirname(HERE), "GoConsoleOS")  # the GoConsoleOS repo
t = np.linspace(0, DUR, int(SR * DUR), endpoint=False)
mix = np.zeros_like(t)

# ---- pad: Am - F - G - Am, whole notes ----
chords = [
    ([110.0, 164.81, 220.0, 261.63, 329.63], 0.0, 12.0),
    ([87.31, 130.81, 174.61, 220.0, 261.63], 11.5, 12.0),
    ([98.0, 146.83, 196.0, 246.94, 293.66], 23.0, 11.0),
    ([110.0, 164.81, 220.0, 261.63, 329.63], 33.5, 11.0),
]
for freqs, start, length in chords:
    seg = np.zeros(int(SR * length))
    tt = np.linspace(0, length, len(seg), endpoint=False)
    for f in freqs:
        for detune in (-0.4, 0.0, 0.4):
            seg += (1.0 / len(freqs)) * np.sin(2 * np.pi * (f + detune) * tt + np.sin(tt * 0.7) * 0.3) * (0.55 + 0.15 * np.sin(tt * 0.13))
    env = np.minimum(1.0, tt / 1.8) * np.clip((length - tt) / 2.5, 0, 1)
    mix[int(start * SR): int(start * SR) + len(seg)] += seg * env * 0.24

# ---- bass sub note on chord roots ----
roots = [(110.0, 0, 12), (87.31, 11.5, 12), (98.0, 23.0, 11), (110.0, 33.5, 11)]
for f, start, length in roots:
    seg_len = int(SR * length)
    tt = np.linspace(0, length, seg_len, endpoint=False)
    seg = np.sin(2 * np.pi * f * tt)
    env = np.minimum(1.0, tt / 0.4) * np.clip((length - tt) / 1.5, 0, 1)
    mix[int(start * SR): int(start * SR) + seg_len] += seg * env * 0.16

# ---- kick: every beat at 92 bpm, soft ----
bpm = 92
beat = 60.0 / bpm
t_kick = 0.0
while t_kick < DUR - 0.5:
    i0 = int(t_kick * SR)
    kt = np.linspace(0, 0.28, int(0.28 * SR), endpoint=False)
    kick = np.sin(2 * np.pi * (130 * np.exp(-kt * 14) + 45) * kt) * np.exp(-kt * 9)
    mix[i0: i0 + len(kick)] += kick * 0.5
    t_kick += beat

# ---- arpeggio: A minor pentatonic 16ths, quiet, with feedback delay ----
scale = [220.0, 261.63, 293.66, 329.63, 392.0, 440.0, 523.25]
pattern = [0, 2, 4, 6, 4, 2, 5, 3, 0, 2, 4, 6, 4, 2, 5, 6]
sixteenth = beat / 4
i = 0
if True:
    t_note = 1.0
    idx = 0
    while t_note < DUR - 1.0:
        f = scale[pattern[idx % len(pattern)]]
        i0 = int(t_note * SR)
        nt = np.linspace(0, 0.16, int(0.16 * SR), endpoint=False)
        note = (np.sin(2 * np.pi * f * nt) + 0.5 * np.sin(2 * np.pi * 2 * f * nt)) * np.exp(-nt * 16)
        end = min(len(mix), i0 + len(note))
        mix[i0:end] += note[: end - i0] * 0.085
        t_note += sixteenth
        idx += 1
# feedback delay by simple echo
echo = np.roll(mix, int(0.28 * SR)) * 0.22
mix = mix + echo * 0.6

# ---- overlay repo sfx hits at scene transitions ----
def read_wav(path):
    with wave.open(path, "rb") as w:
        n = w.getnframes()
        frames = w.readframes(n)
        samps = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
        ch = w.getnchannels()
        if ch > 1:
            samps = samps.reshape(-1, ch).mean(axis=1)
        return samps, w.getframerate()

def overlay(name, at, gain):
    path = os.path.join(REPO, "boot", "system", "sounds", name)
    if not os.path.exists(path):
        return
    samps, sr = read_wav(path)
    i0 = int(at * SR)
    n = min(len(samps), len(mix) - i0)
    if n > 0:
        mix[i0:i0 + n] += samps[:n] * gain

overlay("boot.wav", 0.15, 0.5)
overlay("boot.wav", 4.1, 0.32)
overlay("select.wav", 8.6, 0.45)
overlay("nav.wav", 13.6, 0.4)
overlay("nav.wav", 18.1, 0.4)
overlay("select.wav", 22.6, 0.4)
overlay("nav.wav", 27.1, 0.4)
overlay("nav.wav", 31.1, 0.4)
overlay("launch.wav", 35.1, 0.5)
overlay("success.wav", 39.6, 0.5)

# ---- normalize + master fade in/out ----
mix = mix / (np.max(np.abs(mix)) + 1e-6) * 0.89
fi = int(0.4 * SR)
fo = int(2.2 * SR)
mix[:fi] *= np.linspace(0, 1, fi)
mix[-fo:] *= np.linspace(1, 0, fo)

stereo = np.stack([mix, np.roll(mix, 11) * 0.92], axis=1)
stereo = np.clip(stereo * 32767, -32767, 32767).astype(np.int16)
out = os.path.join(HERE, "trailer_audio.wav")
with wave.open(out, "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(stereo.tobytes())
print("wrote", out, "dur", DUR)
