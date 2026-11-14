"""Проверки репозитория Overworked перед пушем и в CI.

Запуск:
    python tools/checks.py                      # текущая ветка против origin/main
    python tools/checks.py --rev <sha> --branch p2/doors
    python tools/checks.py --ci --base origin/main --branch p2/doors

Ошибки (exit 1) блокируют пуш, предупреждения только печатаются.
Обойти хук целиком можно `git push --no-verify` — только если вы точно знаете, зачем.
"""
import argparse
import os
import re
import subprocess
import sys

# ---------------------------------------------------------------- настройки

ASSETS = "Assets/"

# Кто чем владеет (см. docs/TASKS.md, раздел 1). Пути — префиксы.
# Общие места (Scripts/UI/, Prefabs/, Art/, Data/ и т. п.) никому не приписаны — их можно трогать всем.
# Основные сцены (MainMenu, Floor_03/02/01, WhiteRoom) — общие: в них собирается игра из утверждённых механик.
OWNERS = {
    "p1": [
        "Assets/_Project/Scripts/Core/",
        "Assets/_Project/Scripts/Player/",
        "Assets/_Project/Scenes/Sandbox/Sandbox_P1",
        "ProjectSettings/",
        "Packages/",
    ],
    "p2": [
        "Assets/_Project/Scripts/Interaction/",
        "Assets/_Project/Scripts/Objectives/",
        "Assets/_Project/Scripts/MiniGames/",
        "Assets/Microgames/",
        "Assets/Core/",
        "Assets/_Project/Scenes/Sandbox/Sandbox_P2",
    ],
    "p3": [
        "Assets/_Project/Scripts/Phone/",
        "Assets/_Project/Scripts/Dialogue/",
        "Assets/_Project/Scripts/Story/",
        "Assets/_Project/Scenes/Sandbox/Sandbox_P3",
    ],
    "p4": [
        "Assets/_Project/Scripts/Sanity/",
        "Assets/_Project/Scripts/Heartbeat/",
        "Assets/_Project/Scripts/Hallucinations/",
        "Assets/_Project/Scripts/Flashlight/",
        "Assets/_Project/Scripts/Effects/",
        "Assets/_Project/Scripts/Audio/",
        "Assets/_Project/Scripts/Ending/",
        "Assets/_Project/Scenes/Sandbox/Sandbox_P4",
    ],
}

# Текстовые файлы, в которых ищем маркеры конфликта.
TEXT_EXT = {".cs", ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller",
            ".meta", ".md", ".json", ".asmdef", ".inputactions", ".shader", ".txt", ".yml", ".yaml"}

# Бинарники, которые обязаны идти через Git LFS (как в .gitattributes).
LFS_EXT = {".png", ".jpg", ".jpeg", ".psd", ".tga", ".tif", ".tiff", ".gif", ".bmp", ".exr", ".hdr", ".webp",
           ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".fbx", ".ttf", ".otf", ".mp4", ".mov", ".zip", ".pdf",
           ".dll", ".unitypackage"}

LFS_HEADER = b"version https://git-lfs.github.com/spec/v1"


# ---------------------------------------------------------------- git

def git(*args, binary=False, check=True):
    res = subprocess.run(["git", *args], capture_output=True, check=False)
    if check and res.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)}: {res.stderr.decode('utf8', 'replace').strip()}")
    return res.stdout if binary else res.stdout.decode("utf8", "replace")


def tree_files(rev):
    """Все файлы в коммите rev."""
    out = git("ls-tree", "-r", "--name-only", "-z", rev)
    return [p for p in out.split("\0") if p]


def changed_files(base, rev):
    """Файлы, изменённые в rev относительно общей с base точки (только свои правки ветки)."""
    out = git("diff", "--name-only", "--diff-filter=ACMR", "-z", f"{base}...{rev}")
    return [p for p in out.split("\0") if p]


def blob(rev, path):
    return git("show", f"{rev}:{path}", binary=True)


def ext(path):
    return os.path.splitext(path)[1].lower()


# ---------------------------------------------------------------- проверки

def check_meta(files):
    """1. У каждого ассета и папки в Assets/ есть .meta, у каждого .meta — ассет."""
    errors = []
    # Unity не импортирует файлы и папки, начинающиеся с "." или кончающиеся на "~" (.gitkeep и т. п.)
    def ignored(path):
        return any(part.startswith(".") or part.endswith("~") for part in path.split("/"))

    in_assets = [f for f in files if f.startswith(ASSETS) and not ignored(f)]
    present = set(in_assets)
    dirs = set()
    # Папки берём и из игнорируемых файлов: папка с одним .gitkeep тоже существует и требует .meta.
    for f in files:
        if not f.startswith(ASSETS):
            continue
        parts = f.split("/")
        for i in range(2, len(parts)):  # Assets/A, Assets/A/B …
            if not ignored("/".join(parts[:i])):
                dirs.add("/".join(parts[:i]))
    targets = present | dirs
    for f in in_assets:
        if f.endswith(".meta"):
            if f[:-5] not in targets:
                errors.append(f"лишний .meta без ассета: {f}")
        elif f + ".meta" not in present:
            errors.append(f"нет .meta: {f}")
    for d in sorted(dirs):
        if d + ".meta" not in present:
            errors.append(f"нет .meta у папки: {d}")
    return errors


def check_conflicts(rev, changed):
    """2. Незакрытые маркеры конфликта слияния."""
    errors = []
    for f in changed:
        if ext(f) not in TEXT_EXT:
            continue
        text = blob(rev, f).decode("utf8", "replace")
        if re.search(r"^<<<<<<< ", text, re.M) and re.search(r"^>>>>>>> ", text, re.M):
            errors.append(f"маркеры конфликта (<<<<<<< / >>>>>>>): {f}")
    return errors


def owner_of(path):
    for who, prefixes in OWNERS.items():
        if any(path.startswith(p) for p in prefixes):
            return who
    return None


def check_owners(branch, changed):
    """3–4. Чужие папки, чужие сцены, ProjectSettings/Packages не от P1."""
    m = re.match(r"(p[1-4])/", branch or "")
    if not m:
        return []  # main, docs/…, art/… — зоны не проверяем
    me = m.group(1)
    errors = []
    for f in changed:
        who = owner_of(f)
        if who and who != me:
            errors.append(f"{f} — зона {who.upper()}, а ветка {me.upper()} ({branch}). Попросите владельца.")
    return errors


def check_lfs(rev, changed):
    """5. Бинарники должны быть LFS-указателями, а не файлами целиком."""
    errors = []
    for f in changed:
        if ext(f) in LFS_EXT and not blob(rev, f).startswith(LFS_HEADER):
            errors.append(f"не через Git LFS: {f} (выполните `git lfs install`, затем `git rm --cached` и `git add` заново)")
    return errors


def check_behind(rev, base):
    """6. Ветка отстаёт от main."""
    n = int(git("rev-list", "--count", f"{rev}..{base}").strip() or 0)
    if n:
        return [f"ветка отстаёт от {base} на {n} коммит(ов): сначала `git pull origin main` и проверьте, что всё работает"]
    return []


def check_style(rev, changed):
    """7. Правила CONTRIBUTING — только предупреждения."""
    warns = []
    for f in changed:
        if not f.endswith(".cs") or not f.startswith("Assets/_Project/"):
            continue
        lines = blob(rev, f).decode("utf8", "replace").splitlines()
        for i, line in enumerate(lines):
            s = line.strip()
            if re.search(r"\bTODO\b(?!\()", s):
                warns.append(f"{f}:{i + 1} TODO без имени — пишите TODO(имя): …")
            if re.match(r"//\s*\S.*;\s*$", s) and not s.startswith("///"):
                warns.append(f"{f}:{i + 1} похоже на закомментированный код")
            if re.match(r"(public |internal )?(sealed |abstract |static )*(partial )?class \w+", s):
                above = [l.strip() for l in lines[max(0, i - 6):i]]
                if not any(l.startswith("/// <summary>") for l in above):
                    warns.append(f"{f}:{i + 1} класс без /// <summary>")
    return warns


# ---------------------------------------------------------------- запуск

def current_branch():
    return git("rev-parse", "--abbrev-ref", "HEAD").strip()


def main():
    # Консоль Windows по умолчанию не в UTF-8 — без этого русский вывод падает.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except AttributeError:
            pass

    ap = argparse.ArgumentParser()
    ap.add_argument("--rev", default="HEAD", help="коммит, который проверяем")
    ap.add_argument("--branch", default=None, help="имя ветки (по нему определяется P1–P4)")
    ap.add_argument("--base", default="origin/main", help="с чем сравниваем")
    ap.add_argument("--ci", action="store_true", help="режим GitHub Actions: без проверки отставания")
    args = ap.parse_args()

    branch = args.branch or current_branch()
    rev = args.rev
    base = args.base

    if git("rev-parse", "--verify", "-q", base, check=False).strip() == "":
        print(f"[checks] нет {base} — пропускаю сравнение (первый пуш?)")
        changed = tree_files(rev)
    else:
        changed = changed_files(base, rev)

    errors = []
    errors += check_meta(tree_files(rev))
    errors += check_conflicts(rev, changed)
    errors += check_owners(branch, changed)
    errors += check_lfs(rev, changed)
    if not args.ci and branch != "main" and os.environ.get("OW_SKIP_BEHIND") != "1":
        errors += check_behind(rev, base)
    warns = check_style(rev, changed)

    print(f"[checks] ветка {branch}, изменено файлов: {len(changed)}")
    for w in warns:
        print(f"  предупреждение: {w}")
    for e in errors:
        print(f"  ОШИБКА: {e}")
    if errors:
        print(f"[checks] {len(errors)} ошибок — пуш остановлен. Подробности: CONTRIBUTING.md, раздел «Проверки».")
        return 1
    print("[checks] всё в порядке")
    return 0


if __name__ == "__main__":
    sys.exit(main())
