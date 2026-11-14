# UIManager (HUD, тосты, записка, подсказка управления)

**Владелец:** Программист 3

**Что делает:** общие окна интерфейса и видимость частей HUD. Тосты, текст на весь экран
(записка, системное сообщение), подсказка управления в начале Floor_03, показ/скрытие рассудка,
пульса, телефона и карточки задания.

**Как подключить:** в сцену ничего класть не нужно — `UIManager` на корне `_Systems`, элементы — в `HUD`.
Сборка HUD по частям (каждое меню пересоздаёт только свои объекты, остальное в `_Systems` не трогает):

| Меню Overworked → P3 → | Объекты в HUD |
|---|---|
| Build HUD | `SanityBar` (+ `Fill`), `Heartbeat` (`Icon`, `BPM`), `FullscreenText`, `ControlsHint` |
| Build Phone UI | `PhoneButton`, `PhonePanel`, `Toast` — см. [Phone.md](Phone.md) |
| Build Dialogue UI | `Subtitles` — см. [DialogueManager.md](DialogueManager.md) |

Песочница `Sandbox_P3` (**Build Phone Sandbox**): **N** — записка, **H** — подсказка управления,
**J** — спрятать/показать весь HUD, **K** — тост.

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
- Окна на весь экран блокируют Дэнни, только если он был свободен (если его держит сценарий или
  `NoteInteractable` — управление не трогают). Закрытие — в `LateUpdate`, поэтому то же нажатие E/Esc
  не берёт соседний предмет и не открывает паузу. Открывающее нажатие E окно не закрывает.

**Расположение:** `SanityBar` — левый верх, `Heartbeat` (иконка + BPM) — сразу под ним.

**Для P4:** `SanityBar` уже с компонентом `SanityBarUI` (заглушка) — ему остаётся вести `Fill.fillAmount`
(Image Filled, Horizontal) от `SanityManager.OnSanityChanged`. `Heartbeat` — `Icon` (пульсирует) и `BPM`
(текст); скрипт `HeartbeatUI` положить на объект `Heartbeat`. Карточка задания — своя у P2 (`ObjectiveUI`),
в правом верхнем углу.

**Зависит от:** `PlayerController`, `GameInput` (Interact, Pause), `PhoneManager`, `ObjectiveUI` (P2), `TextFormatter`.

**Известные ограничения / TODO:**
- Порядок онбординга HUD на Floor_03 (что появляется за чем) — `HudOnboarding`, этап 2.
- `HudTester` — только для `Sandbox_P3`.
