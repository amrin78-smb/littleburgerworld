#!/usr/bin/env bash
# Regenerate + validate, Linux test player + autopilot, Android APK.
set -u
cd "$(dirname "$0")/.."
U="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/6000.3.27f2/Editor/Unity}"
[ "${SKIP_SETUP:-0}" = 1 ] || { Tools/unity_batch.sh LittleBurgerWorld.EditorTools.LittleBurgerWorldSetup.RunBatch setup_final; echo "SETUP exit=$?"; }
grep -E "LBW Validate|Vulkan deny|error CS|Could not find" Logs/setup_final.log | head
Tools/unity_batch.sh LittleBurgerWorld.EditorTools.BuildScript.BuildLinuxBatch linux_build -buildTarget Linux64; echo "LINUX exit=$?"
rm -rf Logs/AutoPilot
timeout 600 xvfb-run -a -s "-screen 0 1280x720x24" Builds/Linux/LittleBurgerWorld.x86_64 -force-glcore -screen-fullscreen 0 \
  -screen-width 1280 -screen-height 720 -lbw-autopilot -lbw-shots "$PWD/Logs/AutoPilot" -logFile "$PWD/Logs/autopilot.log"
echo "AUTOPILOT exit=$?"; cat Logs/AutoPilot/autopilot_report.txt 2>/dev/null
"$U" -batchmode -nographics -quit -buildTarget Android -projectPath "$PWD" \
  -executeMethod LittleBurgerWorld.EditorTools.BuildScript.BuildAndroidBatch -logFile Logs/android_build.log
echo "ANDROID exit=$?"; grep "\[LBW Build\]" Logs/android_build.log
