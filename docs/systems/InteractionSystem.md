# InteractionSystem и объекты взаимодействия

**Владелец:** Программист 2

**Что делает:** находит ближайший к Дэнни объект с `IInteractable` (радиус 1.2) и по E вызывает
`Interact()`. `InteractionHighlight` показывает над этим объектом подсказку «[E] <Prompt>» (PT Sans,
белый с тёмной обводкой, едет за объектом) и обводит его спрайты жёлтым контуром. Пока Дэнни
заблокирован (телефон, мини-игра, катсцена, пауза) и у запертых объектов (`CanInteract == false`) —
ни подсказки, ни контура. Настраивать на объектах ничего не нужно.

**Как подключить:** `InteractionSystem` создаёт себя сам при старте игры (DontDestroyOnLoad) —
никуда класть не нужно. Объекту нужен **Collider2D-триггер** и один из компонентов ниже.
Тексты подсказок и тостов — на английском, в полях компонента.

| Компонент | Что делает | Главные поля |
|---|---|---|
| `Inspectable` | описание («ДО») и реплики Дэнни **при подходе**, без E | `dialogue`, `once` |
| `NoteInteractable` | текст на весь экран → тост → реплики | `noteText`, `receivedToast`, `afterDialogue` |
| `PickupInteractable` | предмет исчезает, флаг, тост | `setsFlag` ("MaxPass"), `receivedToast` |
| `DrawerInteractable` | содержимое (описание) → пауза и вторые реплики (по желанию) → флаг, тост | `contentsDialogue`, `afterPauseDialogue`, `setsFlag` |
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

**Сборка этажей по нарративу** (меню **Overworked → P2**, тексты — ассеты `Data/` с именами из таблицы P3):
- **Build Floor_01 Corridor** — главный выход (`F1_MainExit`), дверь 102 на ключ `Key102` с табличкой
  (`F1_Door102Locked`) и заданием `F1_FindKey101`;
- **Build Floor_01 Room 101 Drawer And Key** — тумба в 101: ящик (`F1_Drawer101`) открывает ключ на тумбе,
  подбор ключа — флаг `Key102`, тост, `F1_FindKey101` выполнено, выдаётся `F1_FinishWork`;
- **Place Floor_01 Arcade** — автомат с Tiny UFO в 102;
- **Build Floor_02 Room 201** — шкафы, стол (`F2_Drawer201`, пауза, `F2_Drawer201_Name`), компьютер с мини-игрой 3
  (`F2_Monitor201`); ящик закрывает `F2_CheckDocs201` и выдаёт `F2_CheckComputer201` («Deal with the computer.»),
  компьютер его закрывает; компьютер доступен после ящика (флаг `Docs201Read`);
- **Swap Floor_01 Rooms 101 and 102** — 102 ближе к началу коридора и сверху, 101 дальше и снизу;
- **Build Floor_03 Turnstile And Stairs** — пропуск на кухне (`MaxPass`), турникет с барьером и считывателем,
  лестница заперта до ACCESS GRANTED (`F3_TurnstileDenied` / `F3_TurnstileGranted` / `F3_PassPicked`).

**Сюжет и галлюцинации на объектах P2** — **Overworked → P2 → Wire Story And Hallucinations** (`P2StoryWiring`):
сценарии из шагов P3 в `Story/P2Chain` каждого этажа (пересоздаётся), тексты P3, галлюцинации P4:
- Floor_03: в сценарий P3 у турникета после сообщения Босса — H1 → `F3_H1_After`;
- Floor_02: середина коридора — H2 → `F2_Silhouette` → сбой ввода + `F2_InputDrift` → взгляд в экран + `F2_LookAtScreen`;
  победа на компьютере 201 — H3 → `F2_H3_After` → `F2_Max_02` → `F2_Boss_02` → `F2_Boss02_After`;
- Floor_01: вход в 101 (`Room.onEnter`) — H4 → `F1_H4_After`; ключ — `F1_Max_02` → `F1_Boss_02`;
  у двери 102 с ключом — `F1_Door102Approach`; вход в 102 — `F1_Boss_03` → `F1_Boss03_After`;
  автомат — `F1_ArcadeAfter`, `F1_FinishWork`; выход из 102 после победы — H5 (P4, `FinalScreamerStarter`).

**Флаги** — `GameState.Flags` (`InteractableBase.HasFlag` / `SetFlag`). Используемые: `MaxPass` (пропуск Макса), `Key102` (ключ от 102).

**Публичный API:**

| | |
|---|---|
| `InteractionSystem.Instance.Focused` | объект под подсказкой или null |
| `event OnFocusChanged(IInteractable)` | объект в фокусе сменился |
| `InteractableBase.SetLocked(bool)`, `DoorInteractable.SetDoorLocked(bool)` | из сценария |
| `[ContextMenu] Interact` | проверить объект без игрока |

**Песочница:** меню **Overworked → P2 → Build Interaction Sandbox** собирает `Sandbox_P2`
со всеми объектами (тестовые задания и тексты — в `Scenes/Sandbox/Sandbox_P2_Data`).

**Зависит от:** PlayerController, GameInput, ObjectiveManager, MiniGameHost, DialogueManager
и UIManager (P3 — пока заглушки: тосты и записки пишутся в консоль).

**Известные ограничения / TODO:**
- Карточка задания рисуется своим канвасом; когда у P3 будет место в HUD — перенести в поля `ObjectiveUI`.
- Контур — жёлтая копия спрайта под объектом (unlit, на `outlineThickness` больше с каждой стороны), без
  outline-шейдера: у спрайтов с прозрачными краями он повторяет прямоугольник спрайта.
- TODO(P1): убрать у лестницы собственную надпись `promptLabel` — подсказку теперь рисует InteractionHighlight.
- У лестницы `standaloneE` выключен (P1): E обрабатывает только InteractionSystem.
