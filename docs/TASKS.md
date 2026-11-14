# Overworked — ТЗ для программистов на 1 день

Срок: **1 день (~12 рабочих часов)**. Ниже время указано от старта: `Ч0` — начало работы,
`Ч12` — готовый билд. Если стартуете в 10:00, то Ч4 = 14:00 и т. д.

Источники: [паспорт](PROJECT_PASSPORT.md) (как устроено), [нарратив EN](narrative/NARRATIVE_EN.md)
(тексты для игры), [CONTRIBUTING.md](../CONTRIBUTING.md) (правила кода и git),
[External/TinyUFO](../External/TinyUFO/README.md) (исходники финальной мини-игры).

**Главный принцип дня:** сначала скелет, который проходится от меню до титров на серых
прямоугольниках, потом — мясо. Красота — только после того, как игра проходится целиком.

---

## 1. Роли

| | Зона | Свои папки `Scripts/` | Своя сцена на этапе сборки |
|---|---|---|---|
| **P1** — Core / Player / **интегратор** | ядро, игрок, камера, меню, пролог, сборка билда | `Core/`, `Player/`, `UI/` (MainMenu, Pause, Fade) | `MainMenu` |
| **P2** — Interaction / Objectives / Mini-games | взаимодействия, двери/комнаты, задания, мини-игры 2 и 4 | `Interaction/`, `Objectives/`, `MiniGames/` | `Floor_01` |
| **P3** — Phone / Dialogue / UI / Story | телефон, сообщения, субтитры, HUD, сценарные цепочки, мини-игры 1 и 3 | `Phone/`, `Dialogue/`, `Story/`, `UI/` (HUD, Toast) | `Floor_03` |
| **P4** — Horror | рассудок, пульс, галлюцинации, силуэты, фонарик, свет, PostFX, звук, скример, Белая комната | `Sanity/`, `Heartbeat/`, `Hallucinations/`, `Flashlight/`, `Effects/`, `Audio/` | `Floor_02`, `WhiteRoom` |

**P1 — интегратор:** единственный, кто мержит в `main`, меняет `ProjectSettings/`,
`Packages/`, Build Settings, слои и теги. Остальные просят его в чате.

---

## 2. Git на этот день

- Ветки: `p1/<что>`, `p2/<что>`, `p3/<что>`, `p4/<что>` от свежего `main`.
- **Чужие папки не трогаем.** Нужна правка в чужом скрипте — пишем владельцу.
- **Сцены этажей до этапа 3 не открываем.** Работаем в своей `Scenes/Sandbox/Sandbox_P<N>`.
- Префабы своей системы — в `Prefabs/<папка системы>`; чужие префабы не правим.
- Коммит — минимум раз в час, маленькими кусками; `.meta` вместе с файлами.
- **Синки** (в конце каждого этапа): все пушат свою ветку и открывают PR →
  P1 мержит по очереди → все делают `git pull` из `main` и мержат `main` в свою ветку.
- Сломал `main` — чинишь сразу, всё остальное ждёт.
- Пушить только то, что **компилируется**. Нет ошибок в консоли — можно пушить.
- **Проверки перед пушем** включает каждый один раз: `git config core.hooksPath tools/hooks`.
  Хук не пустит пуш без `.meta`, с конфликтом, в чужую зону, мимо LFS или с отставанием
  от `main` (подробно — [CONTRIBUTING.md](../CONTRIBUTING.md), раздел 6).
- Перед синком — `python tools/check_compile.py` (Unity закрыт). P1 запускает её перед каждым мержем.

---

## 3. Контракты (API между системами)

Чтобы все работали параллельно, P1 на этапе 0 создаёт **заглушки** всех менеджеров
с этими сигнатурами (тело — `Debug.Log`). Каждый потом наполняет свои. **Сигнатуры
без согласования в чате не менять.**

Все менеджеры — синглтоны в префабе `_Systems`: `XxxManager.Instance`.

```csharp
// Core (P1)
GameManager.Instance.State.PlayerName      // string, по умолчанию "Tester"
GameManager.Instance.LoadFloor(int floor)  // 3, 2, 1; 0 = WhiteRoom
FadeController.Instance.FadeOut(float sec, Action done); FadeIn(float sec, Action done)

// Player (P1)
PlayerController.Instance.SetInputLocked(bool locked)
PlayerController.Instance.SetOnlyRight(bool value)          // WhiteRoom
PlayerController.Instance.StartInputDrift(float seconds)     // сбой ввода, Floor_02
PlayerController.Instance.Teleport(Transform point)          // вход/выход из комнат
PlayerController.Instance.PlayAnim(string name)              // "LookAtScreen", "Fall", "LieDown", "StandUp"

// Interaction (P2)
interface IInteractable { string Prompt { get; } bool CanInteract { get; } void Interact(); }

// Objectives (P2)
ObjectiveManager.Instance.StartObjective(ObjectiveData o)
ObjectiveManager.Instance.Complete(ObjectiveData o)
event Action<ObjectiveData> ObjectiveManager.Instance.OnCompleted

// Mini-games (P2)
interface IMiniGame { void Begin(Action<bool> onFinish); }   // true = победа, false = Quit
MiniGameHost.Instance.Open(IMiniGame game, Action<bool> onFinish) // сам блокирует игрока

// Phone (P3)
MessageManager.Instance.Deliver(MessageData msg, Action onReadAndClosed = null)
event Action PhoneManager.Instance.OnClosed

// Dialogue / UI (P3)
DialogueManager.Instance.Play(DialogueData d, Action done = null)   // субтитры и описания
UIManager.Instance.ShowToast(string text)                          // «Received: …»
UIManager.Instance.ShowFullscreenText(string text, Action closed)  // записка, системный текст
UIManager.Instance.SetHudVisible(HudPart part, bool visible)       // Sanity, Heartbeat, Phone, Objective
TextFormatter.Format(string s)                                     // подставляет {name}

// Story (P3)
ScriptedSequence.Play()   // цепочка шагов, настраивается в инспекторе

// Horror (P4)
SanityManager.Instance.SetSanity(float value)          // 0..100, плавно ~1 сек
HeartbeatManager.Instance.Spike(float addBpm, float seconds)
HeartbeatManager.Instance.SetScripted(bool on); SetBpm(float bpm)
HallucinationManager.Instance.Play(HallucinationEvent h, Action done = null)
FlashlightController.Instance.IsOn; SetAvailable(bool); event Action<bool> OnToggled
AudioManager.Instance.PlaySfx(AudioClip c); PlayAmbient(AudioClip c); StopAll()
```

---

## 4. Этапы

### Этап 0 · Ч0–Ч1 · Фундамент

**P1** (все ждут его пуша, это главный блокер дня):
- [x] Unity 6000.5.6f1, шаблон URP 2D, проект в корне репозитория; Force Text, Visible Meta Files,
  только новый Input System. **Сделано заранее** (ветка `setup/unity-project`).
- [x] Скрипты — в namespace `Overworked`, сборка `Overworked.asmdef` (`Assets/_Project/Scripts/`).
- [x] Папки по паспорту (раздел 31), сцены `MainMenu`, `Floor_03/02/01` (иерархия по шаблону этажа),
  `WhiteRoom`, `Sandbox_P1..P4`, Build Settings.
- [x] Заглушки всех классов из раздела 3, префаб `_Systems` (в `Prefabs/Systems/Resources/`), `Bootstrap`.
- [x] Cinemachine 3.1.7; TextMeshPro входит в uGUI.
- [x] Шрифт TextMeshPro с кириллицей и латиницей (в `Fonts/`): PT Sans + IBM Plex Mono.
- [x] Открыть проект, проверить, что любая сцена запускается без ошибок, сообщение в чат «можно пуллить».

Генератор каркаса — меню **Overworked → Setup Project** (`Assets/_Project/Editor/ProjectSetup.cs`):
повторный запуск досоздаёт недостающее и ничего не перезаписывает.

**Все в первые 5 минут:** `git lfs install`, `git config core.hooksPath tools/hooks`, `git pull`,
открыть проект в Unity Hub (Add → папка репозитория; первый импорт — несколько минут).

**P2, P3, P4 пока P1 готовит проект** (без Unity):
- **P2:** изучить исходники в [`External/TinyUFO/`](../External/TinyUFO/README.md): точка входа
  `SpinvMicrogame.Begin`, волны `WaveManager` + `Waves/Wave4.asset`, меню `SpinvMainMenu`.
  Составить список, что выключить (лидерборд, достижения, туториалы, аркада, настройки).
- **P3:** из [NARRATIVE_EN](narrative/NARRATIVE_EN.md) выписать таблицу: все сообщения, реплики, описания, задания → будущие ScriptableObject (имя ассета, этаж, триггер).
- **P4:** собрать звуки (гул ламп, сердцебиение, кардиомонитор, ИВЛ, удары, вибрация телефона, ошибка/успех турникета, стрекот клавиатур) — бесплатные CC0.

**Готово, когда:** у всех проект открывается из `main` без ошибок.

### Этап 1 · Ч1–Ч4 · Базовые системы (в своих Sandbox)

**P1:**
- [x] `PlayerController`: A/D и стрелки, без прыжка, `SetInputLocked`, `Teleport`.
- [x] Камера Cinemachine за игроком + Confiner2D.
- [x] `FadeController`, `SceneTransitionManager`, `GameManager.LoadFloor`.
- [x] `MainMenu`: Start / Quit → позже Continue / New Game / Quit (Continue — после выхода в меню через паузу).
- [x] Пролог: терминал, ввод имени (≤16 символов, не пустое), Enter → Floor_03.

**P2:**
- [ ] `InteractionSystem`: ближайший объект в радиусе, подсказка `[E] <Prompt>`.
- [ ] `Inspectable` (описание при подходе), `NoteInteractable`, `PickupInteractable`, `DrawerInteractable`.
- [ ] `DoorInteractable`: вход/выход из комнаты через Fade + Teleport; запертая дверь с текстом.
- [ ] `ObjectiveManager` + `ObjectiveData` + карточка задания с чекбоксом.

**P3:**
- [x] `MessageData` / `MessageManager` / `PhoneUI`: Tab и кнопка, контакты Max/Boss, история, вибрация + тост.
- [x] `onReadAndClosed`-колбэк после закрытия телефона.
- [x] `DialogueManager`: субтитры Danny и описания (DESC) внизу, по очереди, `{name}`.
- [x] HUD-префаб: места под рассудок, пульс, задание, подсказку, тосты.

**P4:**
- [ ] `SanityManager.SetSanity` + `SanityBarUI` (плавно ~1 сек, без процентов).
- [ ] `HeartbeatManager`: базовый BPM от рассудка, `Spike`, звук удара в такт.
- [ ] `FlashlightController`: 2D Spot Light, F, `SetAvailable`.
- [ ] `AudioManager` + Audio Mixer (Music / Ambient / SFX / Heartbeat).

**Синк 1 (Ч4):** всё в `main`. P1 собирает тестовую сцену: игрок ходит, жмёт E по объекту,
приходит сообщение, падает рассудок, работает фонарик.

### Этап 2 · Ч4–Ч7 · Механики сюжета

**P1:**
- [x] `PlayerController`: `SetOnlyRight`, `StartInputDrift`, `PlayAnim` (пока заглушки-анимации).
- [x] Пауза (Esc).
- [x] Лестница → `LoadFloor`, перенос рассудка/имени/телефона между сценами (проверить!).

**P2:**
- [ ] `MiniGameHost` (оверлей, блок игрока, Esc не ломает).
- [ ] **Мини-игра 2 — Считыватель**: удержать E, отпустить в зелёной зоне.
- [ ] **Мини-игра 4 — Tiny UFO** (по [инструкции](../External/TinyUFO/README.md)):
  1. скопировать `External/TinyUFO/Assets/Core` и `.../Microgames/gc26_spinv` в `Assets/`
     **по тем же путям**, проверить, что `gc26_spinv_Main` запускается как есть — **до Ч5**;
  2. урезать: меню Play/Quit, только `Wave4`, поражение → волна заново;
  3. `TinyUfoMiniGame : IMiniGame` — грузит сцену additive, победа → `true`, Quit → `false`.
- [ ] `TurnstileInteractable`, `ArcadeInteractable`, `ComputerInteractable`, `PersonInteractable` (сжатие в точку).

**P3:**
- [x] `ScriptedSequence`: шаги — реплика, сообщение (ждать прочтения), блок/разблок, анимация,
  показать HUD, галлюцинация, мини-игра, задание, вкл/выкл объект, пауза, fade.
- [ ] `HudOnboarding` (появление HUD по очереди в начале Floor_03), подсказка управления.
- [ ] `NoReturnTrigger` (уведомление «Do you still want Danny…» и остановка).
- [ ] ScriptableObject-ассеты **всех** сообщений, диалогов и заданий из NARRATIVE_EN.

**P4:**
- [ ] `HallucinationManager` + `HallucinationEvent`: звук + визуал + спайк + `SetSanity`.
- [ ] `SilhouetteBehaviour`: исчезает при подходе / свете фонарика.
- [ ] `LightFlicker`, `PostFXController` (виньетка, шум, аберрация, размытие), рывок камеры (Impulse).
- [ ] Заготовки 5 галлюцинаций (H1–H4 и финальная) как префабы.

**Синк 2 (Ч7):** **вертикальный срез** — Floor_03 на серых прямоугольниках проходится
от пролога до лестницы (P3 собирает в своей Sandbox, P1 проверяет).

### Этап 3 · Ч7–Ч10 · Сборка этажей

Каждый открывает **только свою сцену** (раздел 1) и собирает этаж по нарративу из
готовых префабов. Арт подставляется по мере готовности, до этого — прямоугольники.

| Кто | Сцена | Что собрать |
|---|---|---|
| **P3** | `Floor_03` | Мини-игра 1 «Печать отчёта» (провал на 60 %) → онбординг HUD → стол Чарльза → сообщение Макса → кухня + пропуск → турникет (отказ) → сообщение Босса → H1 → считыватель → лестница |
| **P4** | `Floor_02` | Темнота, фонарик обязателен → сообщения → коридор, H2 + сбой ввода + «Was that you?» → Room_201 (ящик, мини-игра 3) → H3 + «DD» → сообщения → запрет возврата → лестница |
| **P2** | `Floor_01` | Коридор (выход, дверь 102) → сообщения → Room_101 (H4, бормочущий, ключ) → Room_102 (Tiny UFO) → финальный скример → WhiteRoom |
| **P4** | `WhiteRoom` | Белый фон, только вправо, реплики, кровать, пульс 40→0, ИВЛ + кардиомонитор, белый fade, титры |
| **P1** | — | Проверка переходов, баги ядра, помощь тому, кто отстаёт; билд-настройки |

Также в этом этапе:
- **P3:** мини-игра 3 «Настройка сигнала» (W/S — выбор ползунка, A/D — значение) для P4.
- **P2:** если Tiny UFO готов раньше — помогает P4 с WhiteRoom.

**Синк 3 (Ч10) — FEATURE FREEZE.** Игра проходится от меню до титров. Новые фичи
больше не добавляются.

### Этап 4 · Ч10–Ч12 · Полировка и билд

- **Все:** проходят игру целиком, баги — в чат с этажом и шагом, чинит владелец системы.
- **P1:** Windows-билд в Ч11 (тестовый) и Ч11:45 (финальный), проверка на чистой машине.
- **P3:** опечатки и `{name}` во всех текстах.
- **P4:** громкость, баланс звука, тайминги галлюцинаций.
- **P2:** сложность Tiny UFO и считывателя (должно проходиться с 1–3 попыток).
- **Все:** `docs/systems/<Система>.md` по своим системам (коротко, по шаблону).

---

## 5. Если не успеваем — режем в этом порядке

1. Необязательные объекты (часы, кулер, кружка, стекло, заявления)
2. Сбой ввода на Floor_02
3. Мини-игры 1 и 3 → заменить катсценой с текстом
4. PostFX и рывки камеры → только мигание света и звук
5. Анимации Danny → статичный спрайт + поворот

**Нельзя резать:** пролог с именем, телефон и сообщения, задания, рассудок и пульс,
турникет, Tiny UFO, скример, Белая комната.

---

## 6. Художники (для справки)

Пока арта нет — у программистов серые прямоугольники нужного размера. Арт сдаётся
к синкам (Ч4, Ч7, Ч10) в `Art/`, заменяет спрайты в префабах. Порядок важности:
Danny (идти/стоять) → коридоры трёх этажей → турникет, двери, ящики, компьютеры →
силуэты и двойник → комнаты → White Room и кровать → необязательные объекты.

---

## 7. Чат и статус

- В начале каждого этапа каждый пишет 1 строку: «P2: беру считыватель и Tiny UFO».
- Застрял больше 20 минут — пишет в чат, не сидит молча.
- Готово → «P4: SanityManager в PR #N».
