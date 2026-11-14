# Phone (PhoneManager, MessageManager, PhoneUI)

**Владелец:** Программист 3

**Что делает:** телефон Дэнни. Сообщения от Max и Boss приходят с вибрацией кнопки и тостом,
история живёт между этажами, ответить нельзя. Пока телефон открыт, Дэнни стоит, мир не на паузе.
После того как игрок прочитал сообщение и закрыл телефон, срабатывает колбэк — на нём держатся
сценарные события (Галлюцинация 1 после сообщения Босса у турникета и т. п.).

**Как подключить:** ничего класть в сцену не нужно — всё живёт в префабе `_Systems`:
`PhoneManager` и `MessageManager` на корне, `PhoneUI` и объекты `PhoneButton` / `PhonePanel` / `Toast` в `HUD`.
UI собирается меню **Overworked → P3 → Build Phone UI** (пересоздаёт только эти три объекта в HUD
и ссылки в `PhoneManager`, `UIManager`). Тексты сообщений — ассеты `MessageData` в `Data/Messages/`
(список — [Texts.md](Texts.md)).

Песочница: **Overworked → P3 → Build Phone Sandbox** собирает `Sandbox_P3` (коридор, игрок, камера,
`PhoneTester`). В Play Mode: **1–3** — доставить тестовые сообщения, **0** — Max и Boss подряд,
**Tab** — телефон, **A/D** — переключить чат, **колесо мыши** — прокрутка, **Esc** — закрыть.

**Публичный API:**

| Метод / событие | Что делает |
|---|---|
| `MessageManager.Instance.Deliver(msg, onReadAndClosed)` | доставить сообщение; колбэк — когда игрок открыл чат этого контакта и закрыл телефон. Тогда же выдаётся `msg.LinkedObjective` |
| `MessageManager.Instance.OnDelivered` / `OnChanged` | новое сообщение / изменились история или непрочитанные |
| `MessageManager.Instance.Clear()` | стереть историю (само вызывается при входе в `MainMenu`) |
| `PhoneManager.Instance.Open()` / `Close()` / `Toggle()` | открыть/закрыть телефон |
| `PhoneManager.Instance.IsOpen`, `OnOpened`, `OnClosed` | состояние и события |
| `PhoneManager.Instance.SetAvailable(bool)` | включить/выключить телефон (онбординг HUD). Обычно через `UIManager.SetHudVisible(HudPart.Phone, v)` — оно прячет и кнопку |
| `UIManager.Instance.ShowToast(text)` | тост сверху по центру; несколько — по очереди; `{name}` подставляется |

**Правила:**
- Цепочка «Макс, следом Босс» — два вызова `Deliver` подряд; при открытии телефон показывает чат
  самого раннего непрочитанного, переключиться — A/D или мышью.
- Если игрок уже заблокирован сценарием, телефон всё равно открывается и после закрытия управление
  **не** возвращает — это делает сценарий. Если игрок был свободен — телефон блокирует и разблокирует сам.
- Не открывается: без игрока (меню), на паузе, во время перехода, в состоянии `Cutscene`,
  во время мини-игры (`MiniGameHost.IsOpen`) и поверх additive-сцены (Tiny UFO). Esc закрывает телефон и не открывает паузу тем же нажатием.

**Зависит от:** `GameInput` (Phone, Pause, Move), `PlayerController`, `PauseMenuUI`, `SceneTransitionManager`,
`ObjectiveManager` (LinkedObjective), `AudioManager` (звук вибрации), `TextFormatter`.

**Известные ограничения / TODO:**
- Во время мини-игр (`MiniGameHost.IsOpen`) телефон не открывается.
- Кнопка телефона видна только когда в сцене есть игрок (`PlayerController.Instance`): в меню и прологе её нет.
  В WhiteRoom HUD скрыт — сцена должна вызвать `UIManager.SetHudVisible(HudPart.Phone, false)`.
- Звук вибрации — поле `vibrateClip` на `MessageManager`, пока пустое (звуки собирает P4).
- В PT Sans нет символов из глитч-сообщений Max на Floor_01 (`▓▒░`, зачёркивание) — понадобится
  fallback-шрифт, проверить при заполнении `F1_Max_*`.
- `PhoneTester` — только для `Sandbox_P3`.
