#!/usr/bin/env bash
# One-time setup + generation of the guided-step voice clips (Kokoro TTS, offline, Apache-2.0).
# Output: Assets/_LittleBurgerWorld/Audio/Voice/<lang>/<key>.wav. Then run Little Burger World > Setup Project.
set -euo pipefail
cd "$(dirname "$0")/../.."
T="${LBW_TTS_HOME:-$HOME/.cache/lbw-tts}"
if [ ! -x "$T/venv/bin/python" ]; then
  python3 -m venv "$T/venv"
  "$T/venv/bin/pip" install -q kokoro-onnx==0.6.1 soundfile
fi
mkdir -p "$T/models"
for f in kokoro-v1.0.onnx voices-v1.0.bin; do
  [ -s "$T/models/$f" ] || curl -sSL -o "$T/models/$f" "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/$f"
done
"$T/venv/bin/python" -I Tools/voice/generate_voice.py Tools/voice/voice_lines.json "${OUT:-Assets/_LittleBurgerWorld/Audio/Voice}" "$T/models" "$@"
