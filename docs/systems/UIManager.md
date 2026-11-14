# UIManager (HUD, тосты, записка, подсказка управления)

**Владелец:** Программист 3

**Что делает:** общие окна интерфейса и видимость частей HUD. Тосты, текст на весь экран
(записка, системное сообщение), подсказка управления в начале Floor_03, показ/скрытие рассудка,
пульса, телефона и карточки задания.

**Как подключить:** в сцену ничего класть не нужно — `UIManager` на корне `_Systems`, элементы — в `HUD`.
Сборка HUD по частям (каждое меню пересоздаёт только свои объекты, остальное в `_Systems` не трогает):

| Меню Overworked → P3 → | Объекты в HUD |
|---|---|
| Build HUD | `FullscreenText`, `ControlsHint`; `SanityBar` и `Heartbeat` — **только если их нет** (их наполняет P4, пересоздание стёрло бы его настройки) |
| Build Phone UI | `PhoneButton`, `PhonePanel`, `Toast` — см. [Phone.md](Phone.md) |
| Build Dialogue UI | `Subtitles` — см. [DialogueManager.md](DialogueManager.md) |

Песочница `Sandbox_P3` (**Build Phone Sandbox**): **N** — записка, **H** — подсказка управления,
**J** — спрятать/показать весь HUD, **K** — тост, **O** — онбординг HUD.

**Публичный API:**

| Метод | Что делает |
|---|---|
| `ShowToast(text)` | тост сверху по центру; несколько — по очереди; `{name}` подставляется |
| `ShowFullscreenText(text, closed)` | текст на весь экран на «бумажке»; закрывается E / Space / Enter / кликом / Esc, затем `closed` |
| `ShowControlsHint(closed)` | подсказка управления (A/D, E, Tab, Esc); закрывается так же |
| `IsFullscreenOpen` | открыто окно — телефон не открывается, субтитры не пропускаются |
| `SetHudVisible(HudPart, bool)` | показать/спрятать часть: `Sanity`, `Heartbeat`, `Phone` (и выключить телефон), `Objective` (карточка P2 через `ObjectiveUI.SetVisible`) |
| `SetHudVisible(bool)` | весь HUD разом (WhiteRoom, скример) |
| `IsHudVisible(HudPart)` | разрешена ли часть сценарием |

**Правила:**
- Рассудок, пульс и кнопка телефона видны только когда в сцене есть игрок — в меню и прологе их нет.
- При смене сцены HUD сбрасывается в «всё видно»: спрятанное сценарием прошлой сцены не переезжает
  на следующий этаж. Сцене, которой HUD не нужен (WhiteRoom), нужно спрятать его в своём Start.
  Открытое окно старой сцены закрывается без колбэка.
- Окна на весь экран блокируют Дэнни, только если он был свободен (если его держит сценарий или
  `NoteInteractable` — управление не трогают). Закрытие — в `LateUpdate`, поэтому то же нажатие E/Esc
  не берёт соседний предмет и не открывает паузу. Открывающее нажатие E окно не закрывает.

**Расположение:** `SanityBar` — левый верх, `Heartbeat` (иконка + BPM) — сразу под ним.

**Для P4:** `SanityBar` уже с компонентом `SanityBarUI` (заглушка) — ему остаётся вести `Fill.fillAmount`
(Image Filled, Horizontal) от `SanityManager.OnSanityChanged`. `Heartbeat` — `Icon` (пульсирует) и `BPM`
(текст); скрипт `HeartbeatUI` положить на объект `Heartbeat`. Карточка задания — своя у P2 (`ObjectiveUI`),
в правом верхнем углу.

**Шрифт окон:** субтитры (реплики Дэнни, описания), тост, записка и подсказка управления — пиксельный
**Hardpixel** (`Fonts/Hardpixel.otf`, TMP-ассет `Hardpixel SDF` создаёт `P3Fonts` при Build Dialogue UI / Build HUD /
Build Phone UI; запасной — PT Sans). Системные строки субтитров — IBM Plex Mono, телефон — Press Start 2P.
⚠ Лицензия Hardpixel не приложена: в файле «© 2010 Anton Kudin & Jovanny Lemonad, All rights reserved» —
перед релизом проверить, разрешено ли встраивать шрифт в игру (сайт автора jovanny.ru).

**Печать текста:** записка и подсказка управления печатаются по буквам со звуком (`windowCharsPerSecond`, 40),
тексты окна — по очереди; первое нажатие E/Space/клик допечатывает сразу, второе закрывает. Тосты
печатаются без звука (`toastCharsPerSecond`, 60). Общий печатник — `Typewriter.Type(text, cps, skip, sound)`
(`Scripts/UI/Typewriter.cs`), им же печатает телефон.

**Звуки интерфейса — `UiSounds`** (`Scripts/UI/UiSounds.cs`, создаёт себя сам, клипы `Audio/UI/Resources/`,
взяты из Tiny UFO): `UI_Typing` — «бегущий текст» по кругу, пока печатаются субтитры (pitch 0.8);
`UI_Click` — на «продолжить» (пропуск строки субтитров, закрытие записки/подсказки) и на нажатие кнопок;
одиночный бип выделения (один удар `UI_Typing`, с 0.08 с клипа) — когда кнопка выделяется стрелками или под неё заводят мышь
(главное меню, пауза, телефон). Кнопки отслеживаются глобально через `EventSystem` — в скриптах меню
ничего добавлять не нужно. API для своих окон: `UiSounds.TypingTick()` / `TypingStop()` / `Click()` /
`SelectBlip()`. Пролог P1 (`PrologueTerminal`) уже озвучен: печать — `TypingTick`, строки лога загрузки — `SelectBlip`, Enter после ввода имени — `Click`. Громкости — в инспекторе объекта `UiSounds` в Play Mode;
звуки идут мимо Audio Mixer и звучат на паузе.

**Зависит от:** `PlayerController`, `GameInput` (Interact, Pause), `PhoneManager`, `ObjectiveUI` (P2), `TextFormatter`.

**Известные ограничения / TODO:**
- Онбординг HUD Floor_03 — `HudOnboarding` (см. [ScriptedSequence.md](ScriptedSequence.md)).
- `HudTester` — только для `Sandbox_P3`.
