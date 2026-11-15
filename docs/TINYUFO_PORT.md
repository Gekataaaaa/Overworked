# Tiny UFO → Overworked: разбор исходников и план переноса

**Владелец:** P2. Результат этапа 0 (Ч0–Ч1) из [TASKS.md](TASKS.md): что внутри Tiny UFO,
что выключить и что сломает Overworked при additive-загрузке. Исходники —
[External/TinyUFO](../External/TinyUFO/README.md). Когда обёртка заработает, из этого файла
делается `docs/systems/TinyUfoMiniGame.md` по шаблону.

> **Статус (этап 2):** перенос сделан — игра в `Assets/Core` и `Assets/Microgames/gc26_spinv`, обёртка
> `TinyUfoMiniGame`, описание — [systems/MiniGameHost.md](systems/MiniGameHost.md). Отличия от плана ниже:
> этаж не нужно строить вдали от начала координат — на время игры `TinyUfoMiniGame` сам выключает
> рендеры, коллайдеры, свет и камеру этажа; Esc в паузе Overworked P1 уже отключил (PR #5);
> `Core/Editor/MicrogameSync.cs` и сборка под Android не перенесены.

## 1. Как устроена игра

Одна сцена `gc26_spinv_Main`, шесть корневых объектов:

| Корень | Что это |
|---|---|
| `Background` | звёздный фон |
| `Main Camera` | камера игры, тег **MainCamera**, `AudioListener`, `CrtCamera` (CRT-проход только на ней) |
| `gc26_spinv` | `SpinvMicrogame` (вход `IMicrogame`), фон и звуки интерфейса (`SpinvAmbience`, `SpinvUiSound`) |
| `GameTemplate` | **выключенный** шаблон партии; по PLAY делается его копия `Game` |
| `Menu` | главное меню автомата, заставка `IntroBoot` |
| `EventSystem` | свой `EventSystem` + `InputSystemUIInputModule` |

Цепочка партии:

```
SpinvMicrogame.Begin(exit)    — только запоминает exit, больше ничего не делает
Menu → PLAY → SpinvMicrogame.StartGame() → Instantiate(GameTemplate)
   WaveManager.Start → RunWave(0) → grid.Build(wave)          (волны из массива waves[])
   волна очищена → OnWaveCleared → последняя? → SpinvGameManager.Victory()
   строй дошёл до низа / жизни кончились → GameOver()
SpinvGameManager.EndGame → Time.timeScale = 0 → событие GameEnded(bool)
   UIManager.ShowEndScreen → VictoryScreen (победа) / GameOverWindow (поражение)
```

**`exit` сейчас никто не вызывает:** по решению автора выход из игры закрывает приложение.
Это главное, что придётся поменять.

Управление: корабль идёт за мышью/пальцем или по A/D и стрелкам и **стреляет сам**.
Способности Z/X/C покупаются за монеты прямо в бою (`AbilityManager`). Пауза — Esc или P.

## 2. Волна 4

`Waves/Wave4.asset` — обычный строй **без босса**: 4×11, 2–5 элитных, скорость 2.7,
выстрел раз в 0.5 с, монетки 30 %. В классике её проходят предпоследней (после Босса 1),
то есть это заметно сложнее первых волн.

Если в `WaveManager.waves` оставить **только `Wave4`**:
- `TotalWaves = 1`, надпись перед волной — `FINAL WAVE`;
- после очистки сразу `Victory()`: сценка «наступает ночь» и поворот часов не запускаются
  (они идут только между волнами);
- редакторские F7/F3 (`RunWave(TotalWaves - 2)`) упадут с индексом −1. Это только редактор,
  ими просто не пользоваться.

Боссы, `BlinkEffect`, `NightCutscene` и аркадные метки на этой волне не срабатывают,
код можно не трогать.

## 3. Что выключить

| Что | Где | Как |
|---|---|---|
| Кнопки TUTORIAL, ARCADE MODE и «?», кубок достижений | `SpinvMainMenu` | выключить объекты кнопок в сцене, листенеры в `Awake` убрать |
| Аркадный режим | `ArcadeMode`, `SpinvMainMenu.OnEnable` | `Play(false)` всегда; ветку `arcade` в `OnEnable` не показывать |
| Обучение | `SpinvTutorial` | при выключенной кнопке не запускается |
| Достижения и плашки | `Achievements`, `AchievementToast`, `StarMapScreen` | кубок и `starMap` выключить, `ShowPendingToasts` убрать. Вызовы `Award` безвредны (только PlayerPrefs) |
| Таблица рекордов и ввод имени | `VictoryScreen`, `HighScoreTable` | экран победы не показывать (раздел 4) |
| HI-SCORE в меню | `SpinvMainMenu` | можно оставить (выглядит как автомат) или выключить подпись |
| Настройки и вибрация | `SpinvSettingsScreen`, `SpinvVolumeSliders`, `SpinvVibrationToggle`, `SpinvHaptics` | шестерёнку в меню выключить. `SpinvHaptics` на ПК ничего не делает |
| Заставка включения `IntroBoot` | `Menu` | **на выбор команды.** Выглядит как включение автомата, но добавляет секунды и «TAP TO START». Предлагаю выключить объект |
| Сборка под Android | `Editor/SpaceInvadersAndroidBuild.cs` | удалить файл |

**Оставить:** волну, корабль, монетки, хилки, способности Z/X/C (часть геймплея, монеты
выпадают на Wave4), CRT, звуки, паузу автомата (с оговоркой из раздела 5).

## 4. Выход: что заменить на `exit`

Сейчас в двух местах вызывается `Application.Quit()` — **в Overworked это закроет всю игру**:

| Где | Сейчас | Нужно |
|---|---|---|
| `SpinvMainMenu.Quit` / `QuitNow` | `QuitEffect` → `Application.Quit()` | `QuitEffect` → `exit(0)` (Quit без выполнения задания) |
| `VictoryScreen` (`QuitGame`) | ввод имени → POWER OFF → `Application.Quit()` | экран победы не показывать вообще |

Как сделать конец партии по нарративу:
- **Победа** → в `UIManager.ShowEndScreen(true)` вместо `victoryScreen.Show` проиграть
  `QuitEffect.Play(() => exit(1))`. Эффект «POWER OFF, экран сплющивается в точку» совпадает
  с нарративом: «после победы экран автомата гаснет».
- **Поражение** → вместо окна GAME OVER с MENU/RESTART: надпись «GAME OVER» / «INVADED» (~1.5 с,
  `WaitForSecondsRealtime`, потому что `timeScale = 0`) → тот же POWER OFF → `exit(2)`
  (`SpinvMicrogame.FinishLost`). Хост (`TinyUfoMiniGame`) считает 1 и 2 сыгранной партией —
  исход на сюжет не влияет; только Quit (`exit(0)`) — выход без выполнения.
  Волна начинается заново, как в нарративе.
- `exit` хранится в `SpinvMicrogame`. Нужен публичный метод, например
  `SpinvMicrogame.Finish(bool won)`, который вызывает `exit(won ? 1 : 0)` один раз.

## 5. Что сломается при additive-загрузке поверх Floor_01

| Проблема | Почему | Решение |
|---|---|---|
| **Две камеры с тегом MainCamera, два AudioListener** | у Overworked своя камера с Cinemachine | на время игры `TinyUfoMiniGame` выключает камеру Overworked (`Camera.main` до загрузки → `SetActive(false)`, после выгрузки → `true`). Tiny UFO везде берёт `Camera.main`: границы корабля, строй, хилки, фон |
| **Камера Tiny UFO снимает спрайты Floor_01** | обе сцены в одном мире, всё Tiny UFO — слой Default, камера около (0, 0), ortho size 9 | **Floor_01 строить в стороне от начала координат** (например, коридор с x ≥ 100). Это моя сцена, правило дешёвое |
| **Два EventSystem** | у Tiny UFO свой | на время игры выключаются EventSystem'ы Overworked, клики обрабатывает родной EventSystem Tiny UFO (с нашим клик по PLAY не доходил); после выхода наши включаются обратно |
| **HUD Overworked рисуется поверх автомата** | HUD Canvas (P3) — overlay | `MiniGameHost` при открытии скрывает HUD (`UIManager.SetHudVisible`) — договориться с P3 |
| **Esc открывает две паузы** | у Tiny UFO своя пауза на Esc/P | P1: `PauseMenuUI` игнорирует Esc, пока `MiniGameHost.IsOpen` |
| **`Time.timeScale = 0`, `AudioListener.pause`** | Tiny UFO ставит их на паузе и в конце партии | после выгрузки сцены вернуть `timeScale = 1` (как в `MicrogameRunner`). Звук Overworked на паузе автомата тоже встаёт — это нормально |
| **Активная сцена** | Tiny UFO спавнит пули и взрывы в активную сцену и чистит их по `gameObject.scene` | после загрузки `SetActiveScene(tiny)`, после выгрузки — обратно `Floor_01` |
| **Портретный экран** | UI рассчитан на 1284×1852, поле считается от камеры | на 16:9 поле станет шире. **Проверить до Ч5.** Если плохо — `Camera.rect` под портрет по центру (вертикальная полоса, как экран автомата) |
| **Ambient и пульс Overworked играют поверх** | `AudioManager` (P4) не знает про мини-игру | по желанию: P4 приглушает Ambient, пока открыт `MiniGameHost` |

Без проблем: свои `PlayerController`, `UIManager`, `GameInput` лежат в namespace `GC26.Spinv`;
ключи PlayerPrefs — `gc26_spinv_*`; слоёв, тегов и sorting layers кроме стандартных нет —
`ProjectSettings` Tiny UFO не нужны.

## 6. Чего нельзя делать

- **Не запускать меню `GC26 → Sync Microgames`** (`Assets/Core/Editor/MicrogameSync.cs`):
  оно **перезаписывает Build Settings целиком** и выкинет сцены Overworked. Предлагаю
  удалить этот файл при переносе (вместе с папкой `Core/Editor/`).
- **Не запускать `Tools → Space Invaders → Build Game`** (`SpaceInvadersBuilder`): он заново
  генерирует сцену и префабы и сотрёт наши правки меню. Сам файл оставить — на `RootDir`
  завязаны пути, но меню не трогать.
- Не переносить по другим путям: только `Assets/Core/` и `Assets/Microgames/gc26_spinv/`.

## 7. Связь с Overworked

`GC26.Core` — `autoReferenced: true`, поэтому код Overworked видит `IMicrogame` без asmdef.
`GC26.Spinv` — `autoReferenced: false`: Overworked **не видит** классы игры и не должен.
Всё общение — через `IMicrogame.Begin(Action<int>)`.

```csharp
// Assets/_Project/Scripts/MiniGames/TinyUfoMiniGame.cs — набросок, не финальный код
public class TinyUfoMiniGame : MonoBehaviour, IMiniGame
{
    const string SceneName = "gc26_spinv_Main";

    public void Begin(Action<bool> onFinish) => StartCoroutine(Run(onFinish));

    IEnumerator Run(Action<bool> onFinish)
    {
        var floor = SceneManager.GetActiveScene();
        var ourCamera = Camera.main;                 // камера Overworked
        yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        var tiny = SceneManager.GetSceneByName(SceneName);
        ourCamera.gameObject.SetActive(false);        // одна MainCamera и один AudioListener
        SceneManager.SetActiveScene(tiny);
        // + выключить EventSystem Tiny UFO, если наш уже есть

        IMicrogame game = null;
        foreach (var root in tiny.GetRootGameObjects())
            if ((game = root.GetComponentInChildren<IMicrogame>(true)) != null) break;

        int? result = null;
        game.Begin(score => result ??= score);       // 1 = победа, 2 = поражение, 0 = Quit
        while (result == null) yield return null;

        yield return SceneManager.UnloadSceneAsync(tiny);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.SetActiveScene(floor);
        ourCamera.gameObject.SetActive(true);
        onFinish(result == 1);
    }
}
```

`MicrogameRunner.Play` из `GC26.Core` делает почти то же, но не возвращает камеру
и активную сцену, поэтому своя обёртка проще.

## 8. Что попросить у других

- **P1:** добавить `Assets/Microgames/gc26_spinv/Scenes/gc26_spinv_Main.unity` в Build Settings
  (это `ProjectSettings/`, зона P1). `PauseMenuUI` — не реагировать на Esc при
  `MiniGameHost.IsOpen`.
- **P3:** `UIManager.SetHudVisible` для всех частей HUD (скрывать на время мини-игры).
- **P4 (по желанию):** приглушать Ambient и пульс, пока открыта мини-игра.

## 9. План на этап 2 (Ч4–Ч7)

1. Скопировать `External/TinyUFO/Assets/Core` и `.../Microgames/gc26_spinv` в `Assets/` по тем же
   путям, удалить `Core/Editor/MicrogameSync.cs` и `SpaceInvadersAndroidBuild.cs`, дождаться импорта,
   запустить `gc26_spinv_Main` как есть. **До Ч5.** Сразу проверить вид на 16:9.
2. Урезать: `waves = [Wave4]`, меню только PLAY/QUIT, без заставки, Quit → `exit(0)`,
   победа → POWER OFF → `exit(1)`, поражение → POWER OFF → `exit(2)`.
3. `TinyUfoMiniGame : IMiniGame` (раздел 7) и `ArcadeInteractable` → `MiniGameHost.Open`.
4. Проверить в `Sandbox_P2`: Quit возвращает без задания, победа закрывает задание, после
   выхода Дэнни ходит, камера, звук и Esc работают, `timeScale = 1`.
5. Сложность: проходится с 1–3 попыток. Если Wave4 слишком тяжёлая — в копии ассета
   снизить `speed` / поднять `missileInterval`, а не трогать код.
