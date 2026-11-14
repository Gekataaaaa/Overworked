# InteractionSystem и объекты взаимодействия

**Владелец:** Программист 2

**Что делает:** находит ближайший к Дэнни объект с `IInteractable` (радиус 1.2), показывает
«[E] <Prompt>» внизу по центру и по E вызывает `Interact()`. Пока Дэнни заблокирован (телефон,
мини-игра, катсцена, пауза), подсказки нет.

**Как подключить:** `InteractionSystem` создаёт себя сам при старте игры (DontDestroyOnLoad) —
никуда класть не нужно. Объекту нужен **Collider2D-триггер** и один из компонентов ниже.
Тексты подсказок и тостов — на английском, в полях компонента.

| Компонент | Что делает | Главные поля |
|---|---|---|
| `Inspectable` | описание («ДО») и реплики Дэнни **при подходе**, без E | `dialogue`, `once` |
| `NoteInteractable` | текст на весь экран → тост → реплики | `noteText`, `receivedToast`, `afterDialogue` |
| `PickupInteractable` | предмет исчезает, флаг, тост | `setsFlag` ("MaxPass"), `receivedToast` |
| `DrawerInteractable` | содержимое (описание) → флаг, тост | `contentsDialogue`, `setsFlag` ("Key102") |
| `DoorInteractable` | вход/выход из комнаты (fade → телепорт → смена камеры → fade) или запертая дверь | `target`, `cameraOn`, `cameraOff`, `doorLocked`, `unlockFlag`, `lockedMessage`, `onLockedTried` |
| `PersonInteractable` | сжимается в точку и исчезает | `visual`, `shrinkTime`, `afterDialogue` |
| `TurnstileInteractable` | своя карта → отказ + `onDenied`; с `passFlag` → считыватель | `cardReader`, `passFlag`, `deniedDialogue` |
| `ComputerInteractable` / `ArcadeInteractable` | запуск мини-игры через `MiniGameHost` | `miniGame` |

**Общие поля (`InteractableBase`)** — есть у всех, кроме `Inspectable`:
- `prompt` — текст после «[E] »;
- `requiresObjective` — доступен, только пока выдано это задание; `requiresFlag` — только при флаге;
- `startLocked` + `SetLocked(bool)` — запереть/открыть из сценария;
- `completesObjective`, `setsFlag` — что сделать после;
- `onDone` (UnityEvent) — сюда сценарий P3 вешает следующее сообщение, галлюцинацию и т. п.

**Комнаты (`Room`):** компонент на `Environment/Rooms/Room_X` — точка входа, точка возврата в коридор,
камера комнаты и коридора, события `onEnter` / `onExit` (под затемнением), `Room.Current` (null — коридор)
и `Room.OnRoomChanged`. Двери достаточно поля `room` и направления (`IntoRoom` у двери в коридоре,
`OutToCorridor` у `ExitDoor`). Камера под затемнением переключается резко (без блендинга Cinemachine).
Поля `target` / `cameraOn` / `cameraOff` — для перехода без комнаты.

**Разводка этажей:** меню **Overworked → P2 → Wire Rooms On All Floors** — Floor_03 (кухня),
Floor_02 (Room_201), Floor_01 (Room_101, Room_102): `Room`, двери входа и выхода, точки возврата.
Декоративные двери коридора (`Door_<число>`) остаются картинками без E. Повторный запуск безопасен.

**Флаги** — `GameState.Flags` (`InteractableBase.HasFlag` / `SetFlag`). Используемые: `MaxPass`, `Key102`.

**Публичный API:**

| | |
|---|---|
| `InteractionSystem.Instance.Focused` | объект под подсказкой или null |
| `event OnFocusChanged(IInteractable)` | для HUD P3, если захочет свою подсказку |
| `InteractableBase.SetLocked(bool)`, `DoorInteractable.SetDoorLocked(bool)` | из сценария |
| `[ContextMenu] Interact` | проверить объект без игрока |

**Песочница:** меню **Overworked → P2 → Build Interaction Sandbox** собирает `Sandbox_P2`
со всеми объектами (тестовые задания и тексты — в `Scenes/Sandbox/Sandbox_P2_Data`).

**Зависит от:** PlayerController, GameInput, ObjectiveManager, MiniGameHost, DialogueManager
и UIManager (P3 — пока заглушки: тосты и записки пишутся в консоль).

**Известные ограничения / TODO:**
- Подсказка «[E] …» и карточка задания рисуются своими канвасами; когда у P3 будет место в HUD —
  перенести в поле `promptText` / поля `ObjectiveUI`.
- У лестницы `standaloneE` выключен (P1): E обрабатывает только InteractionSystem.
