# ObjectiveManager и карточка задания

**Владелец:** Программист 2

**Что делает:** хранит активные задания (может быть несколько сразу — Floor_01: ключ и разговор с сидящим)
и список выполненных, сообщает о выдаче и выполнении. `ObjectiveUI` показывает карточку в правом верхнем углу:
список заданий, у каждого свой чекбокс; выполненное — галочка и через 2.5 с строка уходит, пустая карточка прячется. Длинный текст задания
не переносится и не вылезает за карточку: шрифт строки сам уменьшается (26 → до 16), пока текст не влезет.

**Как подключить:** `ObjectiveManager` — в `_Systems`; `ObjectiveUI` создаёт себя сам.
Задания — ассеты `ObjectiveData` в `Data/Objectives` (`Id`, `Title` на английском, `{name}` работает).
Выдать/закрыть:
- из кода/сценария — `StartObjective` / `Complete`;
- полем `completesObjective` у объекта взаимодействия;
- `ObjectiveTrigger` — зона: выдаёт (или закрывает) задание при входе Дэнни;
- `ObjectiveGiver` — методы `Give()` / `Complete()` для UnityEvent в инспекторе
  (например, `DoorInteractable.onLockedTried` → `Give()`);
- `ObjectiveLink` — вместе с одним заданием выдаёт другое (Floor_01: `F1_FindKey101` → `F1_TalkToMan`).
`requiresObjective` у объекта взаимодействия — «задание выдано и не выполнено» (любое из активных).
`DoorInteractable.unlockAlsoRequires` — ключа мало, нужно ещё выполненное задание (дверь 102 → `F1_TalkToMan`).

**Публичный API:**

| | |
|---|---|
| `StartObjective(ObjectiveData)` | выдать; текущее и уже выполненное повторно не выдаются |
| `Complete(ObjectiveData)` | закрыть (можно и не текущее — запомнится как выполненное) |
| `Active`, `IsActive(o)`, `IsCompleted(o)` | состояние (несколько активных заданий) |
| `Current` | последнее выданное из невыполненных |
| `event OnStarted`, `event OnCompleted` | для UI и сценария |
| `ResetAll()` | новое прохождение |
| `ObjectiveUI.Instance.SetVisible(bool)` | спрятать карточку (онбординг HUD, WhiteRoom) |

**Зависит от:** TextFormatter.

**Известные ограничения / TODO:**
- `ResetAll()` вызывается из `GameManager.ResetState()` при New Game (P1).
- `ObjectiveUI` не знает про `UIManager.SetHudVisible(HudPart.Objective)` — P3 может вызывать
  `ObjectiveUI.SetVisible` оттуда.
