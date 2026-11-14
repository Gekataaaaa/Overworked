# Тексты игры → ScriptableObject

**Владелец:** Программист 3
**Что делает:** список всех текстов из [NARRATIVE_EN.md](../narrative/NARRATIVE_EN.md) и ассетов,
в которые они превращаются. По этой таблице создаются ассеты в `Assets/_Project/Data/`.
Текст строк здесь не дублируется — он берётся из NARRATIVE_EN (ссылка на сцену в колонке «Сцена»).

**Типы ассетов:**
- **M** — `MessageData` (`Data/Messages/`): сообщение в телефон, `Sender`, `Lines[]`, `Floor`, `LinkedObjective`.
- **D** — `DialogueData` (`Data/Dialogues/`): строки подряд `{Speaker: Danny / Description / System, Text}`.
- **I** — `DialogueData` описания объекта (`Data/Inspectables/`): DESC + реплика Дэнни, показывается при подходе.
- **O** — `ObjectiveData` (`Data/Objectives/`): `Id`, `Title`.
- **S** — строка на объекте сцены (подсказка `[E] …`, тост, текст записки/запертой двери) — не ассет,
  поле в инспекторе интерактива (P2) или шага `ScriptedSequence`.

Цепочка `M → M` значит: второе сообщение приходит сразу после первого (паспорт, раздел 14).
«После закрытия» — запускается, когда игрок закрыл телефон (`MessageManager.Deliver(msg, onReadAndClosed)`).

Итого: **12 M** (Max 6, Boss 6), **4 O**, **46 D/I**.

---

## Пролог (MainMenu, делает P1)

| Ассет | Тип | Строки | Триггер |
|---|---|---|---|
| `Prologue_Intro` | D | SYSTEM | после «Start Game» — **уже есть** |
| `Prologue_Welcome` | D | SYSTEM, `{name}` | после ввода имени — **уже есть** |

## Floor_03 (Акт 1) — рассудок 100 → 90

| # | Ассет | Тип | Строки (начало) | Сцена | Триггер / что дальше |
|---|---|---|---|---|---|
| 1 | `F3_Start` | D | DANNY ×3 «Damn… It's late already.» | 1.0 | После провала мини-игры 1 → встаёт → онбординг HUD |
| 2 | `F3_OpenSpace` | D | DANNY ×2 «Everyone left ages ago.» | 1.1 | Зона в опенспейсе (первый проход) |
| 3 | `F3_CharlesDesk` | I | DESC ×3 «Of all the desks…» | 1.1 | Подход к столу Чарльза |
| 4 | `F3_CharlesNote` | S | SYSTEM «THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!» | 1.1 | E — записка на весь экран (`ShowFullscreenText`) |
| 5 | `F3_CharlesNote_After` | D | DANNY ×3 «That's for sure…» | 1.1 | После закрытия записки, затем тост «Received: Charles's note.» |
| 6 | `F3_Max_01` | M | MAX ×5 «Helloooo, Danny.» | 1.1 | Зона выхода в коридор; `LinkedObjective` = `F3_TakeMaxPass` |
| 7 | `F3_Max01_After` | D | DANNY «Fine. At least someone remembered I'm here.» | 1.1 | После закрытия `F3_Max_01` |
| 8 | `F3_TakeMaxPass` | O | «Get Max's pass.» | 1.1 | Выдаётся `F3_Max_01`; закрывается подбором пропуска |
| 9 | `F3_Clock` | I | DESC + DANNY «Battery's dead.» | 1.1 | Необязательный |
| 10 | `F3_Cooler` | I | DESC + DANNY | 1.1 | Необязательный |
| 11 | `F3_EmployeeBoard` | I | DESC + DANNY | 1.1 | Необязательный |
| 12 | `F3_PassPicked` | D | DANNY «Got it.» | 1.2 | Подбор пропуска на кухне → задание выполнено |
| 13 | `F3_Max_02` | M | MAX ×4 «Thanks, buddy!!!» | 1.3 | Зона выхода из кухни (после задания) |
| 14 | `F3_Max02_After` | D | DANNY «Yeah. Good luck to both of us.» | 1.3 | После закрытия `F3_Max_02` |
| 15 | `F3_TurnstileDenied` | D | DESC «ACCESS DENIED.» + DANNY «What?..» | 1.3 | Турникет со своей картой → `F3_Boss_01` |
| 16 | `F3_Boss_01` | M | BOSS ×5 «Danny, have you finished the report?» | 1.3 | После закрытия → **Галлюцинация 1** (P4) |
| 17 | `F3_H1_After` | D | DANNY ×2 «My ears are ringing…» | 1.3 | После H1 |
| 18 | `F3_TurnstileGranted` | D | DESC «ACCESS GRANTED.» + DANNY «It worked. Sorry, Max.» | 1.3 | Победа в мини-игре 2 → лестница доступна |

## Floor_02 (Акт 2) — рассудок 90 → 50

| # | Ассет | Тип | Строки (начало) | Сцена | Триггер / что дальше |
|---|---|---|---|---|---|
| 1 | `F2_Start` | D | DANNY ×2 «So dark…» | 2.0 | Старт этажа, затем подсказка F |
| 2 | `F2_Max_01` | M | MAX ×6 «Danny, wait.» | 2.0 | Первое включение фонарика (`FlashlightController.OnToggled`) |
| 3 | `F2_Boss_01` | M | BOSS ×5 «You're wasting time on the wrong tasks, Danny.» | 2.0 | Цепочка после `F2_Max_01`; `LinkedObjective` = `F2_CheckDocs201` |
| 4 | `F2_CheckDocs201` | O | «Check the documents in room 201.» | 2.0 | Выдаётся `F2_Boss_01`; закрывается после мини-игры 3 |
| 5 | `F2_Boss01_After` | D | DANNY ×2 «Yeah. Like I was ever getting one.» | 2.0 | После закрытия `F2_Boss_01` |
| 6 | `F2_BrokenLamp` | I | DESC + DANNY | 2.1 | Необязательный |
| 7 | `F2_Mug` | I | DESC + DANNY | 2.1 | Необязательный |
| 8 | `F2_Silhouette` | D | DANNY ×2 «What was that?..» | 2.1 | После **Галлюцинации 2** (P4) |
| 9 | `F2_InputDrift` | D | DANNY ×2 «Wait…» | 2.1 | Во время сбоя ввода (`StartInputDrift`) |
| 10 | `F2_LookAtScreen` | D | DANNY ×2 «Was that you?» | 2.1 | Дэнни поворачивается к экрану (`PlayAnim("LookAtScreen")`) |
| 11 | `F2_Drawer201` | D | DESC ×2 + DANNY ×2, `{name}` | 2.2 | Ящик в Room_201 |
| 12 | `F2_Drawer201_Name` | D | DANNY ×2 «Wait. That's your name, isn't it?» | 2.2 | После паузы за `F2_Drawer201` |
| 13 | `F2_Monitor201` | D | SYSTEM «REPORT NOT CLOSED. / SHIFT CONTINUES.» | 2.2 | Победа в мини-игре 3 → задание выполнено → **Галлюцинация 3** |
| 14 | `F2_H3_After` | D | DANNY «What the hell is going on here?!» | 2.2 | После H3 |
| 15 | `F2_Max_02` | M | MAX ×4 «Danny, did you find the papers??» | 2.2 | После H3 и открытия двери |
| 16 | `F2_Boss_02` | M | BOSS ×6, `{name}` «You see what happens…» | 2.2 | Цепочка после `F2_Max_02` |
| 17 | `F2_Boss02_After` | D | DANNY ×2 «No… No, I don't want to go there.» | 2.2 | После закрытия `F2_Boss_02` |
| 18 | `F2_NoReturn` | D | SYSTEM «Do you still want Danny to keep working here?» + DANNY «I just want to go home.» | 2.2 | `NoReturnTrigger` позади игрока |

## Floor_01 (Акт 3) — рассудок 50 → 0

| # | Ассет | Тип | Строки (начало) | Сцена | Триггер / что дальше |
|---|---|---|---|---|---|
| 1 | `F1_MainExit` | I | DESC + DANNY «There's nothing out there…» | 3.0 | E на главном выходе |
| 2 | `F1_Door102Locked` | D | DESC «The door is locked…» | 3.0 | E на двери 102 (пока нет ключа) → выдать `F1_FindKey101` |
| 3 | `F1_FindKey101` | O | «Find the key in room 101.» | 3.0 | Выдаётся сценой после `F1_Door102Locked`; закрывается ящиком в 101 |
| 4 | `F1_Max_01` | M | MAX ×4 (глитч) | 3.0 | Зона через несколько шагов |
| 5 | `F1_Boss_01` | M | BOSS ×7, `{name}` «Look at him, {name}.» | 3.0 | Цепочка после `F1_Max_01` |
| 6 | `F1_DannyToPlayer` | D | DANNY ×4, `{name}` «Did you hear him, {name}?» | 3.0 | После закрытия `F1_Boss_01`, Дэнни смотрит в экран |
| 7 | `F1_DarkGlass` | I | DESC + DANNY | 3.0 | Необязательный |
| 8 | `F1_Forms` | I | DESC + DANNY | 3.0 | Необязательный |
| 9 | `F1_H4_After` | D | DANNY ×2 «Stop it…» | 3.1 | После **Галлюцинации 4** (вход в Room_101) |
| 10 | `F1_PersonGone` | D | DANNY «He wasn't here…» | 3.1 | Бормочущий исчез (`PersonInteractable`) |
| 11 | `F1_Drawer101` | D | DESC `{name}` + DANNY ×2 | 3.1 | Ящик → тост «Received: Room 102 key.» → `F1_FindKey101` выполнено → `F1_FinishWork` |
| 12 | `F1_FinishWork` | O | «Finish work in room 102.» | 3.1 | Выдаётся сценой после ящика; закрывается победой в Tiny UFO |
| 13 | `F1_Max_02` | M | MAX ×3 (глитч, двоичный код) | 3.1 | После выдачи `F1_FinishWork` |
| 14 | `F1_Boss_02` | M | BOSS ×5, `{name}` «Excellent, {name}.» | 3.1 | Цепочка после `F1_Max_02` |
| 15 | `F1_Door102Approach` | D | DANNY ×2 «The break room? Now?» | 3.2 | Подход к двери 102 с ключом |
| 16 | `F1_Boss_03` | M | BOSS ×4 «Sit down, Danny.» | 3.2 | Вход в Room_102 |
| 17 | `F1_Boss03_After` | D | DANNY «Never thought I'd hear that from him.» | 3.2 | После закрытия `F1_Boss_03` |
| 18 | `F1_ArcadeAfter` | D | DANNY ×2 «That's it?» | 3.2 | Победа в Tiny UFO → `F1_FinishWork` выполнено |
| 19 | `F1_Screamer_No` | D | DANNY «No…» | 3.3 | Скример: Дэнни замирает перед двойником |
| 20 | `F1_Screamer_Fall` | D | DANNY ×2, `{name}` «{name}…» | 3.3 | Дэнни падает, затем звук обрывается → WhiteRoom |

## WhiteRoom (Акт 4)

| # | Ассет | Тип | Строки (начало) | Триггер |
|---|---|---|---|---|
| 1 | `WR_01` | D | DANNY ×2, `{name}` «{name}… / Are you still here?» | Через паузу после старта |
| 2 | `WR_02` | D | DANNY ×2 «I don't understand where we are.» | Зона по ходу вправо |
| 3 | `WR_Bed` | D | DANNY «I think I know this place.» | Появилась кровать, Дэнни останавливается |
| 4 | `WR_03` | D | DANNY ×2 «I kept thinking…» | Зона ближе к кровати |
| 5 | `WR_04` | D | DANNY «And then I could go home.» | После короткой паузы |
| 6 | `WR_05` | D | DANNY «Funny. I can't even remember…» | Зона у кровати; пульс 40 → 10 |
| 7 | `WR_06` | D | DANNY ×2, `{name}` «Thank you for staying with me till the end.» | Перед тем как Дэнни ложится |

---

## Строки на объектах (S)

Не ассеты — поля в инспекторе. Подсказки `[E] …` задаются на интерактивах P2, тосты — `UIManager.ShowToast`.

| Этаж | Подсказки `[E]` | Тосты / тексты |
|---|---|---|
| 3 | Take the note · Pick up the pass · Use your card · Use Max's card · Go down to the second floor | Received: Charles's note. · This doesn't look like the kitchen. (запертые двери) |
| 2 | Enter room 201 · Check the computer · Open the drawer · Go down to the first floor | Подсказка F: Turn the flashlight on / off |
| 1 | Enter room 101 · Interact · Open the drawer · Unlock and enter room 102 · Play | Received: Room 102 key. |
| Все | — | NEW MESSAGE FROM MAX / NEW MESSAGE FROM THE BOSS — тост телефона, генерируется из `Sender` |

## Вопросы и что не хватает в коде

1. **`OnReadEvent` у `MessageData`.** Паспорт (разделы 14, 23) описывает поле `OnReadEvent`, но в
   контракте `Deliver(msg, onReadAndClosed)` событие после прочтения передаётся колбэком.
   Предложение: поле не добавлять, а реакцию после прочтения (H1, реплики «_After») делать шагом
   `ScriptedSequence` «сообщение → ждать закрытия → следующий шаг». **Решено:** поля нет, паспорт
   поправлен (см. [Phone.md](Phone.md)).
2. **Задания без сообщения.** `F1_FindKey101` и `F1_FinishWork` выдаются сценой, а не сообщением —
   нужен шаг `ScriptedSequence` «выдать задание» (он и так в списке шагов).
3. **Пауза внутри диалога.** У `DialogueData` нет паузы между строками, поэтому реплики с паузой
   разбиты на два ассета (`F2_Drawer201` / `F2_Drawer201_Name`, `WR_03` / `WR_04`), пауза — шаг сценария.
4. **SYSTEM-строки на этажах** (`F2_Monitor201`, `F2_NoReturn`) показываются через `DialogueManager`
   со `Speaker = System` — нужен отдельный стиль (моноширинный шрифт), это задача HUD.
