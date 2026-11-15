# AudioManager

**Владелец:** Программист 4

**Что делает:** один на игру, четыре канала — Music, Ambient, SFX, Heartbeat. Под каждый создаёт
свой AudioSource и пускает его через группу миксера `Settings/MainMixer`.
Фон при смене плавно затухает и проявляется.

**Как подключить:** уже лежит в `_Systems`, группы миксера назначены. Громкость каналов —
в миксере (Window → Audio → Audio Mixer) или полями `…Volume` у AudioManager.
Звуки класть в `Audio/<Канал>/` (паспорт, раздел 20).

**Публичный API:**

| Метод | Что делает |
|---|---|
| `PlaySfx(AudioClip)`, `PlaySfx(AudioClip, float volume)` | короткий звук, можно несколько сразу |
| `PlaySfxHeld(AudioClip, float volume, bool loop = false)` → `AudioSource` | звук на своём источнике до `StopSfx` (или пока не доиграет); незацикленный переживает смену сцены (шаги по лестнице), зацикленный (шипение помех) гаснет при смене сцены; `StopAll` гасит все |
| `SetSfxVolume(AudioSource, float volume)` | громкость звука из `PlaySfxHeld` на ходу (шипение следует силе помех) |
| `PlaySfxPitched(AudioClip, float volume, float pitch)` | короткий звук со своей высотой тона (шаги — разброс тона); такие звуки не должны перекрываться |
| `StopSfx(AudioSource, float fade)` | погасить звук из `PlaySfxHeld` за `fade` с (0 — сразу) |
| `PlayAmbient(AudioClip)` | зацикленный фон с плавной сменой; тот же клип не перезапускается; null — выключить |
| `PlayMusic(AudioClip)` | музыка; null — выключить |
| `PlayHeartbeat(AudioClip, float volume)` | удар сердца (вызывает HeartbeatManager) |
| `StopHeartbeat()` | оборвать звучащий удар сердца (выход в меню) |
| `StopAll()` | мгновенно остановить всё (конец скримера) |

**Зависит от:** ничего.

**Пересобрать заготовки:** меню **Overworked → P4 → Build Horror Sandbox** — создаёт миксер (если нет)
и назначает группы в `_Systems`, тёмный коридор в `Sandbox_P4` (шкала и пульс — из HUD P3 в `_Systems`).
Если Unity не даст создать миксер автоматически — Create → Audio Mixer `Settings/MainMixer`,
группы Music / Ambient / SFX / Heartbeat под Master, и снова запустить меню.

**Известные ограничения / TODO:**
- TODO(P4): гул ламп и стук клавиатуры в конце пролога (MainMenu.md).
- TODO(P4): искажение фона на низком рассудке (паспорт, раздел 12).
