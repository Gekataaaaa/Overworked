# Tiny UFO — исходники для финальной мини-игры

Копия PC-версии Tiny UFO (коммит `4206957` в приватном репозитории автора) — только то,
что нужно, чтобы встроить игру в Overworked и вытащить из неё 4-ю волну.

Папка лежит **вне `Assets/`**, поэтому Unity-проект Overworked её не видит и не компилирует.
Это исходник для переноса, а не рабочая копия: правки делаются уже в `Assets/` Overworked.

## Что здесь

```
External/TinyUFO/
├── Assets/
│   ├── Core/                       # GC26.Core: IMicrogame, MicrogameRunner — интерфейс запуска мини-игры
│   └── Microgames/gc26_spinv/      # сама игра, namespace GC26.Spinv, asmdef GC26.Spinv
│       ├── Scripts/                # геймплей, UI, звук
│       ├── Editor/                 # SpaceInvadersBuilder — генерирует сцену и префабы
│       ├── Prefabs/                # игрок, пришельцы, боссы, снаряды
│       ├── Waves/Wave1..5.asset    # данные волн; нужна Wave4.asset
│       ├── Scenes/gc26_spinv_Main.unity
│       ├── Sprites/ Audio/ Shaders/ Settings/ (CRT)
│       └── microgame.json
└── Packages/manifest.json          # для справки: какие пакеты нужны
```

## Зависимости

Пакеты (есть в шаблоне URP 2D): URP 17.5, Input System 1.20, uGUI 2.5, TextMeshPro.
Unity — та же 6000.5.6f1.

## Как перенести в Overworked (задача P2)

1. Скопировать `Assets/Core/` и `Assets/Microgames/gc26_spinv/` в `Assets/` Overworked
   **по тем же путям** — `SpaceInvadersBuilder.RootDir` жёстко указывает на
   `Assets/Microgames/gc26_spinv`, а `.meta` сохранят ссылки между ассетами.
2. Дождаться импорта, проверить, что в консоли нет ошибок.
3. Открыть `gc26_spinv_Main.unity` — игра должна запускаться как есть.
4. Урезать до мини-игры Overworked:
   - меню: только **Play** и **Quit** (`SpinvMainMenu`, `UIManager`);
   - одна волна — `Waves/Wave4.asset` (`WaveManager`; в редакторе F7 сразу запускает 4-ю волну —
     хорошая точка входа);
   - победа в волне → `onFinish(true)`, Quit → `onFinish(false)`, поражение → волна заново;
   - убрать лидерборд, достижения, туториалы, аркадный режим, настройки, вибрацию.
5. Обернуть в `IMiniGame` Overworked (`TinyUfoMiniGame`), запуск — через `MiniGameHost`.
   Самый простой путь: грузить `gc26_spinv_Main` **additive**-сценой поверх `Floor_01`
   и выгружать после окончания. Точка входа игры — `SpinvMicrogame.Begin(exitCallback)`.

## Осторожно

- В Tiny UFO есть свои `PlayerController`, `UIManager`, `GameInput` — они в namespace
  `GC26.Spinv`, поэтому с классами Overworked не конфликтуют. Скриптам Overworked
  не нужно ничего из `GC26.Spinv` знать, кроме точки входа.
- Ключи `PlayerPrefs` начинаются с `gc26_spinv_` — с Overworked не пересекаются.
- CRT-эффект сделан через `CrtCamera` (отдельная камера), а не через renderer feature —
  общий `Renderer2D` Overworked трогать не нужно.
- Спрайты и звуки хранятся в Git LFS. После клона: `git lfs install` и `git lfs pull`,
  иначе спрайты будут белыми.
