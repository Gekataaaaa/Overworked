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
- TODO(P1): анимации Дэнни, когда будет арт.
