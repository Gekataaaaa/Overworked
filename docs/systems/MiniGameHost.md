# MiniGameHost, считыватель пропуска и Tiny UFO

**Владелец:** Программист 2 (мини-игры 1 и 3 — P3, через тот же `IMiniGame`)

**Что делает:** `MiniGameHost` открывает мини-игру, блокирует Дэнни, прячет карточку задания
и по окончании возвращает управление. Одна мини-игра за раз.

**Как подключить:** `MiniGameHost` — в `_Systems`. Мини-игра — компонент с `IMiniGame` на объекте;
его отдают в поле `miniGame` (`ComputerInteractable`, `ArcadeInteractable`) или
`cardReader` (`TurnstileInteractable`). Новая мини-игра:

```csharp
public class MyGame : MonoBehaviour, IMiniGame
{
    public void Begin(Action<bool> onFinish) { /* показать оверлей; потом onFinish(true/false) */ }
}
```

**Мини-игра 2 — `CardReaderMiniGame`:** оверлей из кода. Пока E зажата, ползунок ходит вверх-вниз
(отражается от краёв); отпустил в зелёной зоне — ACCESS GRANTED, мимо — повтор. Esc — выход.
Настройки: `sliderSpeed`, `zoneSize` (сложность), тексты.

**Мини-игра 4 — `TinyUfoMiniGame`:** грузит `gc26_spinv_Main` additive, на время игры выключает
камеру, рендеры, коллайдеры и 2D-свет сцены этажа и HUD-канвасы Overworked, после выгрузки возвращает
ровно то, что выключил, и `timeScale`/звук. Сама игра урезана: меню PLAY/QUIT, одна волна (Wave4),
победа → «WAVE CLEARED» → POWER OFF → `onFinish(true)`, поражение → волна заново, QUIT → `onFinish(false)`.
Достижения, аркада, обучение, настройки, таблица рекордов и заставка выключены. Подробности —
[TINYUFO_PORT.md](../TINYUFO_PORT.md). В редакторе F9 в Tiny UFO — мгновенная победа.

**Публичный API:**

| | |
|---|---|
| `MiniGameHost.Instance.Open(IMiniGame, Action<bool>)` | true — победа, false — выход |
| `MiniGameHost.Instance.IsOpen` | идёт мини-игра |
| `event OnOpenChanged(bool)` | P3 — спрятать HUD, P4 — приглушить звук |

**Зависит от:** PlayerController, GameInput (E, Esc), ObjectiveUI, GC26.Core (`IMicrogame`).

**Известные ограничения / TODO:**
- `gc26_spinv_Main` добавлена в Build Settings (P1).
  Без этого Tiny UFO запускается только в редакторе (грузится по пути).
- Не запускать **Tools → Space Invaders → Build Game**: пересоздаст сцену Tiny UFO и сотрёт урезание.
- Интерфейс Tiny UFO рассчитан на портрет; на 16:9 поле шире — проверить на глаз.
