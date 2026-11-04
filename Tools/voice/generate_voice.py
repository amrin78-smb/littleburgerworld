"""Generate voice clips for Little Burger World with Kokoro TTS (Apache-2.0, runs offline on CPU).

Usage: generate_voice.py <voice_lines.json> <output_dir> <models_dir> [keys...]
Writes <output_dir>/<lang>/<key>.wav (24 kHz, 16-bit mono): silence trimmed, soft fades,
peak-normalised, short lead-in so the first syllable is never clipped on Android.
"""
import json
import os
import sys

import numpy as np
import soundfile as sf
from kokoro_onnx import Kokoro

LANG_CODES = {"en": "en-us", "es": "es"}


def tidy(audio, sr):
    audio = np.asarray(audio, dtype=np.float32)
    thr = 0.01 * float(np.max(np.abs(audio)) or 1.0)
    idx = np.where(np.abs(audio) > thr)[0]
    if len(idx):
        a = max(0, idx[0] - int(0.03 * sr))
        b = min(len(audio), idx[-1] + int(0.12 * sr))
        audio = audio[a:b]
    fade = int(0.012 * sr)
    if len(audio) > 2 * fade:
        audio[:fade] *= np.linspace(0, 1, fade)
        audio[-fade:] *= np.linspace(1, 0, fade)
    peak = float(np.max(np.abs(audio)) or 1.0)
    audio = audio * (0.89 / peak)  # about -1 dBFS
    lead = np.zeros(int(0.06 * sr), dtype=np.float32)
    return np.concatenate([lead, audio])


def main():
    lines_path, out_dir, models = sys.argv[1], sys.argv[2], sys.argv[3]
    only = set(sys.argv[4:])
    cfg = json.load(open(lines_path, encoding="utf-8"))
    tts = Kokoro(os.path.join(models, "kokoro-v1.0.onnx"), os.path.join(models, "voices-v1.0.bin"))
    for lang, voice in cfg["voices"].items():
        speed = float(cfg.get("speed", {}).get(lang, 0.9))
        os.makedirs(os.path.join(out_dir, lang), exist_ok=True)
        for line in cfg["lines"]:
            if only and line["key"] not in only:
                continue
            text = line.get(lang)
            if not text:
                continue
            audio, sr = tts.create(text, voice=voice, speed=speed, lang=LANG_CODES.get(lang, lang))
            audio = tidy(audio, sr)
            path = os.path.join(out_dir, lang, line["key"] + ".wav")
            sf.write(path, audio, sr, subtype="PCM_16")
            print(f"{lang} {line['key']:<16} {len(audio) / sr:4.1f}s  {text}")


if __name__ == "__main__":
    main()
