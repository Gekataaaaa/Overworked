# WhiteRoomSequence

**Владелец:** Программист 4

**Что делает:** сцена `WhiteRoom` целиком (паспорт, раздел 10). На входе — тишина (`AudioManager.StopAll`),
HUD скрыт, пульс Белой комнаты (`HeartbeatManager.EnterWhiteRoom`, 70 BPM), Дэнни идёт только вправо.
Через `introDelay` — первые реплики. По пути `WhiteRoomTrigger` показывают реплики, проявляют кровать
и на секунду останавливают Дэнни («замечает кровать»). У кровати Дэнни ложится сам: управление
заблокировано, удары сердца больше не звучат (канал `Audio_Heartbeat` заглушён, при выходе из сцены звук
возвращается), пульс гаснет до нуля за `heartFadeTime`, через `hospitalSoundsDelay` проявляется писк
кардиомонитора на каждый удар сердца (и ИВЛ на канале Ambient — только если назначен настоящий звук). Сердце остановилось → непрерывный сигнал →
экран растворяется в белом → титры → `GameManager.LoadMainMenu()`.

**Как подключить:** меню **Overworked → P4 → Build White Room** — собирает сцену: линия пола, кровать,
Дэнни, камера, триггеры, объект `WhiteRoom` со сценарием и `WhiteRoomUI` (белый слой + титры, Canvas
с порядком 90 — под HUD и чёрным затемнением `_Systems`). Объекты сборщика пересоздаются;
`FloorSettings`, `Story`, `Main Camera` и свет каркаса не трогаются.

Реплики — ассеты `Data/Dialogues/WR_01`…`WR_06` и `WR_Bed` (имена — по таблице P3 [Texts.md](Texts.md), текст из NARRATIVE_EN, сцена 4.0), титры — `WR_Credits`
(строки через перенос, поддерживает `{name}`). Готовые ассеты сборщик не перезаписывает — правьте их в инспекторе.

**Компоненты:**

| Компонент | Что делает |
|---|---|
| `WhiteRoomSequence` | вход, `Say(DialogueData)`, `LieDown()`, финал; `InBed` — Дэнни уже лёг |
| `WhiteRoomTrigger` | точка на пути (Collider2D, Is Trigger): реплики, `reveal` (проявить спрайты), `holdPlayer` (остановка, сек), `lieDown` |
| `HospitalSounds` | заглушки звуков: писк монитора и непрерывный сигнал (генерируются при старте) |

**Настройки (`WhiteRoomSequence`):** `introDelay`, `heartFadeTime` (14 с), `hospitalSoundsDelay`,
`flatlineBeforeWhite`, `whiteFadeTime`, `creditsTime` (пропуск любой клавишей после половины),
свои клипы `monitorBeep`, `ventilatorLoop`, `flatlineLoop` (пусто — заглушки; `ventilatorLoop` пусто — ИВЛ не звучит), громкости.

**Автотест:** `Tests/PlayMode/WhiteRoomTests` — виртуальная клавиатура держит D: Дэнни не идёт влево, идёт вправо,
доходит до кровати, пульс гаснет до 0, после титров грузится MainMenu, пульс снова обычный. Test Runner → PlayMode
или `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter WhiteRoomTests` (редактор закрыт).

**Отладка:** F5 (`DebugHotkeys`) — загрузить WhiteRoom; или открыть сцену и нажать Play (`_Systems`
поднимает Bootstrap). Идти вправо до кровати (~21 с). Клавиатура работает, только когда окно **Game** в фокусе — кликните по нему после Play. Только пульс — клавиша B в `Sandbox_P4`.

**Зависит от:** HeartbeatManager (`EnterWhiteRoom`, `FadeBpm`, `OnBeat`, `Bpm`), AudioManager,
PlayerController (`SetOnlyRight`, `SetInputLocked`, `Teleport`, `PlayAnim("LieDown")`),
DialogueManager и UIManager (`SetHudVisible`) — P3, SceneTransitionManager, GameManager.

**Известные ограничения / TODO:**
- **Сейчас включён отладочный режим сборщика** (`DebugMode = true` в `WhiteRoomBuilder`): кровать (на своём месте,
  x = 32) видна сразу, на полу метки через 2 юнита (без них в белом не видно, что Дэнни идёт — камера едет за ним),
  шкала рассудка и пульс из HUD P3 (`_Systems` → HUD) не прячутся (`debugShowHorrorHud`) — видно, как пульс гаснет до 0.
  Перед финальной сборкой поставить `false` и пересобрать сцену — по паспорту HUD в WhiteRoom скрыт.
- Пока нет анимации `LieDown`, спрайт-заглушка Дэнни просто поворачивается на бок.
- `DialogueManager` пока заглушка (пишет в консоль) — субтитры появятся, когда его заполнит P3.
- Заглушка ИВЛ (сгенерированный шум) убрана — шипела. Если найдётся спокойный CC0-звук ИВЛ — назначить в `ventilatorLoop`.
- Писк и непрерывный сигнал пока сгенерированы — можно заменить CC0-звуками в инспекторе `WhiteRoom`.
- Арт BG-14 (фон) и BG-15 (кровать) — заменить спрайты в `Environment/Bed`.
- При уходе в меню сценарий возвращает пульс в обычный режим и рассудок 100 — как при первом запуске.
- Имена в `WR_Credits` — заглушки «Programmer 1…», вписать команду.
