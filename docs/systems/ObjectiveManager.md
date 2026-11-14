# ObjectiveManager и карточка задания

**Владелец:** Программист 2

**Что делает:** хранит текущее задание (одно за раз) и список выполненных, сообщает о выдаче
и выполнении. `ObjectiveUI` показывает карточку в правом верхнем углу (под пульсом):
заголовок и чекбокс; после выполнения — галочка и через 2.5 с карточка прячется.

**Как подключить:** `ObjectiveManager` — в `_Systems`; `ObjectiveUI` создаёт себя сам.
Задания — ассеты `ObjectiveData` в `Data/Objectives` (`Id`, `Title` на английском, `{name}` работает).
Выдать/закрыть:
- из кода/сценария — `StartObjective` / `Complete`;
- полем `completesObjective` у объекта взаимодействия;
- `ObjectiveTrigger` — зона: выдаёт (или закрывает) задание при входе Дэнни;
- `ObjectiveGiver` — методы `Give()` / `Complete()` для UnityEvent в инспекторе
  (например, `DoorInteractable.onLockedTried` → `Give()`).

**Публичный API:**

| | |
|---|---|
| `StartObjective(ObjectiveData)` | выдать; текущее и уже выполненное повторно не выдаются |
| `Complete(ObjectiveData)` | закрыть (можно и не текущее — запомнится как выполненное) |
| `Current`, `IsCompleted(o)` | состояние |
| `event OnStarted`, `event OnCompleted` | для UI и сценария |
| `ResetAll()` | новое прохождение |
| `ObjectiveUI.Instance.SetVisible(bool)` | спрятать карточку (онбординг HUD, WhiteRoom) |

**Зависит от:** TextFormatter.

**Известные ограничения / TODO:**
- `ResetAll()` вызывается из `GameManager.ResetState()` при New Game (P1).
- `ObjectiveUI` не знает про `UIManager.SetHudVisible(HudPart.Objective)` — P3 может вызывать
  `ObjectiveUI.SetVisible` оттуда.
