"""Проверка, что проект Overworked компилируется в Unity (batchmode, без окна).

Запуск (редактор Unity с этим проектом должен быть ЗАКРЫТ):
    python tools/check_compile.py
    python tools/check_compile.py --unity "D:/Unity/6000.5.6f1/Editor/Unity.exe"

Путь к Unity можно задать и переменной окружения UNITY_PATH.
Занимает 1–3 минуты (первый раз дольше — импорт). Запускать перед синками,
P1 — перед каждым мержем в main.
"""
import argparse
import os
import re
import subprocess
import sys

DEFAULT_UNITY = r"C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOG = os.path.join(ROOT, "Logs", "check_compile.log")


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass

    ap = argparse.ArgumentParser()
    ap.add_argument("--unity", default=os.environ.get("UNITY_PATH", DEFAULT_UNITY))
    args = ap.parse_args()

    if not os.path.isfile(args.unity):
        print(f"[compile] Unity не найден: {args.unity}\n"
              f"          Укажите путь: --unity <путь к Unity.exe> или UNITY_PATH")
        return 2
    if not os.path.isdir(os.path.join(ROOT, "Assets")):
        print("[compile] в репозитории ещё нет Unity-проекта (папки Assets) — нечего проверять")
        return 0
    if os.path.exists(os.path.join(ROOT, "Temp", "UnityLockfile")):
        print("[compile] проект открыт в Unity — закройте редактор и запустите снова")
        return 2

    os.makedirs(os.path.dirname(LOG), exist_ok=True)
    print("[compile] Unity компилирует проект, это 1–3 минуты…")
    code = subprocess.call([args.unity, "-batchmode", "-nographics", "-quit",
                            "-projectPath", ROOT, "-logFile", LOG])

    with open(LOG, encoding="utf8", errors="replace") as f:
        log = f.read()
    # Ошибки компилятора C# выглядят так: Assets/…/X.cs(12,5): error CS0103: …
    errors = sorted(set(re.findall(r"^.*\.cs\(\d+,\d+\): error CS\d+:.*$", log, re.M)))
    for e in errors:
        print("  " + e)
    if errors or code != 0:
        print(f"[compile] ошибки компиляции: {len(errors)}, код выхода Unity {code}. Полный лог: {LOG}")
        return 1
    print("[compile] проект компилируется без ошибок")
    return 0


if __name__ == "__main__":
    sys.exit(main())
