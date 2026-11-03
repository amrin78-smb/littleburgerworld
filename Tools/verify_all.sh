#!/usr/bin/env bash
# Full verification: EditMode + PlayMode tests, then Linux autopilot and Android APK (Tools/full_pipeline.sh).
set -u
cd "$(dirname "$0")/.."
U="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/6000.3.27f2/Editor/Unity}"
for P in EditMode PlayMode; do
  xvfb-run -a -s "-screen 0 1920x1080x24" "$U" -batchmode -force-glcore -projectPath "$PWD" -runTests -testPlatform $P \
    -testResults "$PWD/Logs/${P,,}-results.xml" -logFile "Logs/${P,,}.log"
  echo "$P exit=$? $(grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Logs/${P,,}-results.xml | head -1)"
done
SKIP_SETUP=1 Tools/full_pipeline.sh
