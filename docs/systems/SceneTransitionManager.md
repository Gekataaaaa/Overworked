# SceneTransitionManager и FadeController

**Владелец:** Программист 1

**Что делает:** меняет сцены через затемнение: блок игрока → fade out → асинхронная загрузка →
кадр на `Start()` объектов сцены → fade in. `FadeController` — чёрная панель поверх всего HUD.

**Как подключить:** ничего класть не нужно — оба живут в префабе `_Systems` (создаёт `Bootstrap`).
Сцена должна быть в Build Settings.

**Публичный API:**

| Метод / событие | Что делает |
|---|---|
| `GameManager.Instance.LoadFloor(int)` | 3, 2, 1; 0 = WhiteRoom (обычный способ перейти) |
| `GameManager.Instance.LoadMainMenu()` | в главное меню (титры, пауза) |
| `SceneTransitionManager.Instance.Load(string)` | любая сцена по имени; во время перехода вызов игнорируется |
| `SceneTransitionManager.Instance.IsLoading` | идёт переход |
| `event OnSceneLoaded(string)` | сцена загружена, экран ещё чёрный |
| `FadeController.Instance.FadeOut(sec, done)` / `FadeIn(sec, done)` | затемнение/проявление (unscaled time, работает на паузе) |
| `FadeController.Instance.SetBlack(bool)` | мгновенно чёрный / прозрачный экран |
| `FadeController.Instance.IsBlack` | экран полностью чёрный |

Пример — вход в комнату: `FadeOut(0.4f, () => { player.Teleport(entry); FadeIn(0.4f, null); })`.

**Лестница** — префаб `Prefabs/Environment/Staircase` (`StairInteractable`, `Scripts/Core/`):
`targetFloor` — куда ведёт, `SetLocked(bool)` — запереть/открыть из сценария, `Prompt` — текст
подсказки. E и подсказку обрабатывает `InteractionSystem` (P2). Галочка **`standaloneE`** (выключена)
нужна только в тестовых сценах без InteractionSystem: тогда лестница сама ловит E.
По E играет звук спуска `descendSound` (в префабе — `Audio/SFX/SFX_StairsDown`): через
`AudioManager.PlaySfxFading` он идёт поверх затемнения, `descendHold` (2.5 с) в полную громкость и
затухает за `descendFade` (1.5 с) уже на новом этаже.

**Каркасы этажей** — меню **Overworked → P1 → Build Floor Skeletons**: коридор, двери с подписями,
комнаты под коридором (EntryPoint, ExitDoor, выключенная камера комнаты), игрок, камера, лестница.
Уже собранный этаж (есть объекты в `Environment/Corridor/Background`) пропускается.

**Отладка (только редактор и dev-билд):** F1 — MainMenu, F2/F3/F4 — Floor_03/02/01,
F5 — WhiteRoom, F6 — fade туда-обратно, F7 — панель состояния, F8 — по кругу коридор → комнаты этажа → коридор
(без запуска сценариев комнат; `Scripts/Core/DebugHotkeys.cs`).

**Зависит от:** GameManager, PlayerController (блок ввода на время fade out).

**Известные ограничения / TODO:**
- Повторный `FadeOut`/`FadeIn` прерывает текущий и продолжает с текущей прозрачности.
- Sandbox-сцены не в Build Settings — из F-клавиш в них не вернуться (останови Play).
