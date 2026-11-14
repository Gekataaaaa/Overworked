# ScriptedSequence (сценарные сцены)

**Владелец:** Программист 3

**Что делает:** короткая постановочная сцена как цепочка шагов, которые выполняются по очереди
(паспорт, раздел 17). Каждый шаг ждёт своего окончания: реплики — пока покажутся, сообщение — пока
игрок прочитает и закроет телефон, мини-игра — пока закончится. Из таких сценариев собираются этажи.

**Как подключить:**
1. Пустой объект в группе `Story` сцены этажа → компонент `ScriptedSequence`.
2. В инспекторе **Add step** → выбрать тип шага, заполнить поля. Порядок — кнопки ↑ ↓, удалить — ✕.
3. Запуск — одно из:
   - `playOnStart` — сам при старте сцены (начало этажа);
   - `SequenceTrigger` на объекте с `Collider2D` (Is Trigger) — когда Дэнни входит в зону;
   - из кода / UnityEvent: `sequence.Play()` (например, `onFinished` другого сценария или шаг `PlaySequence`).

Пример — `StoryDemo` в `Sandbox_P3` (жёлтая зона справа от старта; меню **Overworked → P3 → Build Phone Sandbox**).

**Настройки сценария:** `once` — проиграть один раз; `lockPlayer` — Дэнни стоит всё время сценария,
в конце управление возвращается; `onFinished` — UnityEvent в конце.

**Шаги** (`Scripts/Story/Steps/`):

| Шаг | Что делает | Ждёт |
|---|---|---|
| Dialogue | реплики / описание / системный текст (`DialogueManager`) | показ всех строк (`Wait`) |
| Message | сообщение в телефон | прочтение и закрытие телефона (`WaitUntilRead`) |
| Lock Player | заблокировать / вернуть управление | — |
| Play Anim | `PlayerController.PlayAnim` (LookAtScreen, Fall, LieDown, StandUp) | `WaitSeconds` |
| Hud | показать / спрятать часть HUD или весь | — |
| Controls Hint | подсказка управления | закрытие |
| Fullscreen Text | текст на весь экран | закрытие |
| Toast | тост | — |
| Objective | выдать / закрыть задание | — |
| Wait Objective | — | пока задание не выполнят |
| Set Active | включить / выключить объект сцены | — |
| Wait | пауза (игровое время) | `Seconds` |
| Fade | в чёрное / из чёрного (`FadeController`) | конец затемнения |
| Hallucination | `HallucinationManager.Play` (P4) | конец галлюцинации (`Wait`) |
| Mini Game | `MiniGameHost.Open`, `Game` — компонент с `IMiniGame` | конец; `UntilWin` — повторять до победы |
| Input Drift | сбой ввода на N секунд (Floor_02) | `Wait` |
| Teleport | поставить Дэнни в точку | — |
| Event | любые вызовы через UnityEvent (открыть дверь `SetDoorLocked(false)`, свет…) | — |
| Play Sequence | запустить другой сценарий | `Wait` |
| Hud Onboarding | онбординг HUD Floor_03 (`HudOnboarding`) | закрытие подсказки управления |

Цепочка «Макс, следом Босс»: `Message` (Max, **без** WaitUntilRead) → `Message` (Boss, с WaitUntilRead).

**Публичный API:**

| Метод / событие | Что делает |
|---|---|
| `Play()` / `Play(Action done)` | запустить с первого шага (повторно не запускается, если идёт или `once` и уже был) |
| `Stop()` | оборвать на текущем шаге без `done` |
| `IsPlaying`, `HasPlayed`, `Finished` | состояние и событие конца |
| `ScriptedSequence.WaitCallback(start)` | помощник для своих шагов: ждать API в стиле `Action done` |

**HudOnboarding** (Floor_03, сцена 1.0): компонент в группе `Story`. С `hideOnStart` прячет рассудок,
пульс и телефон при старте этажа; шаг **Hud Onboarding** показывает рассудок + пульс → телефон →
подсказку управления (паузы `firstDelay`, `stepDelay`, звук `appearClip`) и ждёт, пока игрок её закроет.
Дэнни стоит всё это время. Пример начала Floor_03:
`Mini Game` (печать отчёта) → `Dialogue` F3_Start → `Play Anim` StandUp → `Hud Onboarding`.
В песочнице — клавиша **O**.

**SequenceTrigger:** `requireCompleted` — срабатывать только после выполнения задания (сообщение
после кухни), `blockIfCompleted` — не срабатывать после него, `once`.

**Новый тип шага:** класс-наследник `SequenceStep` с `[Serializable]` в `Scripts/Story/Steps/`,
поля — публичные с `[Tooltip]`, `Describe()` — подпись, `Run()` — корутина. В меню Add step появится сам.

**Зависит от:** `DialogueManager`, `MessageManager`, `UIManager`, `PlayerController`, `ObjectiveManager`,
`FadeController`, `HallucinationManager`, `MiniGameHost`.

**Известные ограничения / TODO:**
- Время шагов игровое: на паузе сценарий стоит.
- Сценарий не переживает смену сцены (живёт в сцене этажа).
- Сценарий, который заблокировал Дэнни шагом `Lock Player`, обязан вернуть управление сам (или `lockPlayer`).
