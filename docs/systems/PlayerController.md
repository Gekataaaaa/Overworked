# PlayerController

**Владелец:** Программист 1

**Что делает:** двигает Дэнни влево-вправо (A/D, стрелки; без прыжка и бега) через Rigidbody2D,
поворачивает спрайт, умеет блокировать ввод, телепортировать игрока, «только вправо» и сбой ввода.

**Как подключить:** положить в сцену префаб `Prefabs/Player/Player` (Rigidbody2D, BoxCollider2D,
PlayerController, PlayerAnimator, дочерний `Sprite`). Стены коридора — объекты с `BoxCollider2D`.
Камера: `CinemachineCamera` с `Follow = Player`, `CinemachinePositionComposer` (Lookahead) и
`CinemachineConfiner2D`; границы — `PolygonCollider2D` (Is Trigger), **по высоте больше кадра**
(иначе Confiner2D прибивает камеру к центру). Готовый пример — `Sandbox_P1`
(собирается меню **Overworked → P1 → Build Player Sandbox**).

**Публичный API:**

| Метод / свойство | Что делает |
|---|---|
| `PlayerController.Instance` | игрок текущей сцены (у каждой сцены свой) |
| `SetInputLocked(bool)` | блок/разблок ввода; не счётчик — последний вызов побеждает |
| `Teleport(Transform)` | мгновенно ставит игрока в точку (вход/выход из комнаты) |
| `SetOnlyRight(bool)` | WhiteRoom: движение только вправо |
| `StartInputDrift(float sec)` | сбой ввода: sec секунд Дэнни идёт дальше после отпускания клавиши |
| `PlayAnim(string)` | "LookAtScreen", "Fall", "LieDown", "StandUp" — состояние Animator с таким именем |
| `State`, `Facing`, `IsMoving` | режим (Normal/Locked/Cutscene), направление (±1), идёт ли |

**Ввод для всех систем — `GameInput`** (`Scripts/Core/GameInput.cs`), карта `Player` в
`Settings/InputSystem_Actions`:

| Действие | Клавиша | Кто использует |
|---|---|---|
| `GameInput.Move` | A/D, ←/→ | PlayerController |
| `GameInput.Interact` | E | InteractionSystem, считыватель (`IsPressed()` для удержания) |
| `GameInput.Flashlight` | F | FlashlightController |
| `GameInput.Phone` | Tab | PhoneManager |
| `GameInput.Pause` | Esc | меню паузы |

Пример: `if (GameInput.Interact.WasPressedThisFrame()) …`

**Зависит от:** Input System (project-wide actions), Cinemachine 3 (камера в сцене).

**Известные ограничения / TODO:**
- `PlayAnim` работает, только когда у Дэнни появится Animator с состояниями (арт).
- Y игрока заморожен — этажи плоские; перемещение между этажами — сменой сцены.
- Пауза — `PauseMenuUI` (Esc), камера для этажей — префаб `Prefabs/Player/PlayerCamera` (+ `FollowPlayer`).
  Пауза не открывается, пока игрок заблокирован и пока поверх этажа загружена additive-сцена
  (Tiny UFO со своей паузой на Esc).
  Вид — как у главного меню (P1 → Build Pause Menu): белая надпись PAUSE от руки в манере логотипа
  (`Art/UI/Logo/Pause_Title.png`), пункты Resume / Main Menu / Quit — `ConsoleMenuItem` без фона, зелёным цветом
  терминала пролога (выбранный ярче), звуки `UiSounds`. На паузе HUD (канвас `HUD` в `_Systems`) и карточка заданий прячутся, после — возвращаются.
- TODO(P1): анимации Дэнни, когда будет арт.

## Фонарик в руке (HeldFlashlight)

На корне префаба Player (ставит **Overworked → P1 → Setup Held Flashlight**). Спрайт `Art/Props/Flashlight_Held.png`
(PPU 1088, пивот на рукоятке) держится в кисти правой руки по кадрам анимации, зеркалится с Дэнни.
Виден, когда `FlashlightController.Available` (Floor_02 — после подбора). Слои: Дэнни → фонарик →
кисть (`HandOverlay`, картинка «только кисть» этого кадра из `Art/Characters/Danny/Hands`) — пальцы
обхватывают рукоятку, а фонарик не прячется за телом. Свет P4 переносится в линзу,
при включённом фонарике линза светится (`LensGlow`, Unlit, жёлтый). Точки кисти по кадрам — массив
`handPoints` в инспекторе (пиксели кадра 300 × 430 от левого верхнего угла): если фонарик «отрывается»
от руки на каком-то кадре — поправить там.

## Дэнни смотрит в экран (LookAtScreen)

`PlayerController.LookAtScreen()` (или шаг сценария `PlayAnimStep` с `LookAtScreen`): Дэнни останавливается,
сбой ввода сбрасывается, показывается фронтальный кадр `Danny_LookAtScreen` (лицом к игроку; ставит PlayerAnimator
поверх анимации), первые `lookMinSeconds` (1.2 с) не прерывается, дальше — пока идут реплики; потом управление
возвращается, если его не забрал кто-то ещё. Если управление вернул кто-то другой (закрыли лист документа, мини-игру) и Дэнни
пошёл — взгляд заканчивается сразу; на ходу фронтальный кадр не показывается никогда.
Пока Дэнни смотрит в экран, фонарика в руке нет и луч погашен (`HeldFlashlight` выключает сам источник света,
не `Toggle()` — без щелчка и `OnToggled`); после взгляда фонарик снова в руке, луч — в прежнем состоянии (`IsOn`). Плюс автоматически: список `lookAtScreenDialogues` в префабе
(ставит **Overworked → P1 → Setup Look At Screen**: Floor_02, Floor_01, WhiteRoom; кроме мест, где он лежит) — на репликах Дэнни из этих диалогов он сам смотрит в экран,
даже если диалог запускает чужой объект (ящик в 201, запрет возврата). Добавить новое место — дописать
диалог в список (сборщик `Editor/LookAtScreenSetup.cs`).

## Дэнни ложится в кровать (LieDown, WhiteRoom)

`PlayAnim("LieDown")` (вызывает `WhiteRoomSequence` у кровати): PlayerAnimator один раз проигрывает кадры
`lieDownFrames` (`Art/Characters/Danny/LieDown/Danny_LieDown_0..7`, нарезка ленты `Denny_LieDown_strip`,
`lieDownFps` = 5) и держит последний. Кровать нарисована в каждом кадре — ровно там же, где спрайт кровати в сцене
(тот же тон и материал), поэтому сцену не прячем. Пока лежит (`IsLyingDown`), взгляд в экран не включается
и фонарика в руке нет. Любое другое `PlayAnim` (например, "StandUp") снимает режим.
Фон, кровать, точку `BedPoint` и кадры ставит **Overworked → P1 → Dress White Room** (`Editor/WhiteRoomArt.cs`)
после пересборки сцены билдером P4.
