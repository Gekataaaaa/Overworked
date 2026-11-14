using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Общая игровая цепочка (интегратор P1): расставляет в основных сценах механики, которые уже есть
    /// в main, чтобы после New Game игра проходилась по нарративу, а не по тестовым сценам.
    /// Сейчас — Floor_03 из механик P3: вступление за столом (печатает, реплики, встаёт — арт Danny_Seated),
    /// стартовые реплики, онбординг HUD, подсказка управления,
    /// реплики в опенспейсе, стол Чарльза, сообщения Макса (с заданием) и Босса.
    /// Floor_02 и Floor_01 — фонарик P4 в руках у Дэнни с начала этажа (Floor_02 затемнён).
    /// Всё кладётся в группу Story/Chain (и Interactables/Chain) — повторный запуск пересобирает только её,
    /// владелец этажа может потом переложить сценарии к себе и удалить группу.
    /// Запуск: меню Overworked → Build Game Chain → Floor 03 (P3 mechanics).
    /// </summary>
    public static class ChainBuilder
    {
        const string Scenes = "Assets/_Project/Scenes/";
        const string Data = "Assets/_Project/Data/";
        const float FloorY = -4.2f;   // как в FloorSkeletonBuilder
        const string ChainGroup = "Chain";

        [MenuItem("Overworked/Build Game Chain/Floor 03 (P3 mechanics)")]
        public static void BuildFloor03()
        {
            EnsureFloor03Dialogues();
            var scene = EditorSceneManager.OpenScene(Scenes + "Floor_03.unity", OpenSceneMode.Single);
            var story = Group(Root("Story"));
            var props = Group(Root("Interactables"));

            // Ассеты грузим после OpenScene: открытие сцены выгружает загруженное раньше (см. MainMenuBuilder).
            var start = Load<DialogueData>("Dialogues/F3_Start");
            var openSpace = Load<DialogueData>("Dialogues/F3_OpenSpace");
            var max01After = Load<DialogueData>("Dialogues/F3_Max01_After");
            var h1After = Load<DialogueData>("Dialogues/F3_H1_After");
            var max02After = Load<DialogueData>("Dialogues/F3_Max02_After");
            var charlesDesk = Load<DialogueData>("Inspectables/F3_CharlesDesk");
            var max01 = Load<MessageData>("Messages/F3_Max_01");
            var max02 = Load<MessageData>("Messages/F3_Max_02");
            var boss01 = Load<MessageData>("Messages/F3_Boss_01");
            var takePass = Load<ObjectiveData>("Objectives/F3_TakeMaxPass");

            // 1.0 Начало. TODO(P3): перед репликами — мини-игра 1 «Печать отчёта», когда будет готова.
            var opening = Sequence(story, "1.0 Opening", playOnStart: true, lockPlayer: true, OpeningSteps(start));

            // 1.1 Опенспейс — реплики при первом проходе.
            Zone(story, "1.1 OpenSpace", -15f, 2f, null,
                new DialogueStep { Dialogue = openSpace });

            // 1.1 Стол Чарльза — описание при подходе (Inspectable). Записка по E — P2 (NoteInteractable).
            // Сам стол нарисован на фоне этажа — заглушку-прямоугольник не создаём, только зону осмотра.
            var deskZone = new GameObject("CharlesDesk_Inspect");
            deskZone.transform.SetParent(props.transform, false);
            deskZone.transform.position = new Vector3(-10f, FloorY + 1.5f, 0);
            var deskCol = deskZone.AddComponent<BoxCollider2D>();
            deskCol.isTrigger = true;
            deskCol.size = new Vector2(3.5f, 4f);
            var inspect = deskZone.AddComponent<Inspectable>();
            Set(inspect, "dialogue", charlesDesk);

            // Порядок этажа (решение команды, отличается от нарратива): кухня заперта →
            // турникет со своей картой: ACCESS DENIED → Босс → Макс про пропуск (задание) → кухня открывается →
            // пропуск → выход из кухни: Макс благодарит → турникет с картой Макса → лестница.
            var kitchenDoor = FindKitchenDoor();
            if (kitchenDoor != null)
            {
                // Не «It's locked.»: до задания Макса дверь вообще не взаимодействует — без подсказки и обводки
                // (startLocked у InteractableBase → CanInteract = false). Откроет сценарий через SetLocked(false).
                kitchenDoor.SetDoorLocked(false);
                var so = new SerializedObject(kitchenDoor);
                so.FindProperty("startLocked").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Chain] Floor_03: не найдена дверь в кухню (DoorInteractable → Kitchen)");

            // Пропуск Макса (P2) должен закрывать задание: в сцене P2 ссылка пустая — его сборщик (Floor03Turnstile)
            // грузит ассет до OpenScene, и тот выгружается. Карточка задания после выполнения уйдёт сама (ObjectiveUI).
            foreach (var pickup in Object.FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(pickup);
                if (so.FindProperty("setsFlag").stringValue != "MaxPass") continue;
                so.FindProperty("completesObjective").objectReferenceValue = takePass;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 1.3 Выход из кухни после пропуска — второе сообщение Макса.
            // Сработает, когда пропуск подобран (Pickup с completesObjective = F3_TakeMaxPass).
            Zone(story, "1.3 MaxThanks", 9f, 3f, takePass,
                new MessageStep { Message = max02, WaitUntilRead = true },
                new DialogueStep { Dialogue = max02After });

            // Турникет P2: своя карта → ACCESS DENIED → onDenied → Босс, следом Макс про пропуск
            // (сообщение выдаёт задание F3_TakeMaxPass), ответ Дэнни, кухня открывается.
            // TODO(P4): Галлюцинация 1 — шаг Hallucination после сообщения Босса.
            var unlockKitchen = new EventStep { Note = "Unlock kitchen" };
            if (kitchenDoor != null)
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(unlockKitchen.OnRun,
                    new UnityEngine.Events.UnityAction<bool>(kitchenDoor.SetLocked), false);
            var boss = Sequence(story, "1.3 DeniedBossThenMax", playOnStart: false, lockPlayer: true,
                new MessageStep { Message = boss01, WaitUntilRead = true },
                // Нарратив 1.3 (P3): после сообщения Босса — Галлюцинация 1 (P4, H1_Turnstile) и «My ears are ringing…».
                new HallucinationStep { Hallucination = FindHallucination("H1_Turnstile") },
                new DialogueStep { Dialogue = h1After },
                new MessageStep { Message = max01, WaitUntilRead = true },
                new DialogueStep { Dialogue = max01After },
                unlockKitchen);
            var turnstile = Object.FindAnyObjectByType<TurnstileInteractable>();
            if (turnstile != null)
            {
                // Убираем старые подписки цепочки (после пересборки их цели удалены) и подписываем заново.
                for (int i = turnstile.onDenied.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    var target = turnstile.onDenied.GetPersistentTarget(i);
                    if (target == null || target is ScriptedSequence)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(turnstile.onDenied, i);
                }
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(turnstile.onDenied,
                    new UnityEngine.Events.UnityAction(boss.Play));
                EditorUtility.SetDirty(turnstile);
            }
            else
            {
                // Турникета ещё нет — запасной вариант: зона перед лестницей.
                Zone(story, "1.3 BossAtTurnstile", 17f, 2f, null,
                    new MessageStep { Message = boss01, WaitUntilRead = true });
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Chain] Floor_03: собрано ({opening.name} + зоны)");
        }

        /// <summary>
        /// Шаги 1.0 Opening. Если в сцене есть арт сидящего Дэнни (Corridor/Props/Danny_Seated с детьми
        /// Typing / Idle / StandUp): Дэнни за своим столом — подсказка управления — печатает — перестаёт и сидит —
        /// стартовые реплики — встаёт — вместо арта появляется игрок — HUD по частям.
        /// Без арта — прежний вариант: Дэнни стоит, реплики, HUD, подсказка управления.
        /// </summary>
        static SequenceStep[] OpeningSteps(DialogueData start)
        {
            var seated = FindInScene("Danny_Seated");
            var playerSprite = FindPlayerSprite();
            GameObject typing = Child(seated, "Typing"), idle = Child(seated, "Idle"), standUp = Child(seated, "StandUp");
            var hud = new SequenceStep[]
            {
                new HudStep { Part = HudPart.Sanity, Visible = true },
                new HudStep { Part = HudPart.Heartbeat, Visible = true },
                new WaitStep { Seconds = 0.5f },
                new HudStep { Part = HudPart.Phone, Visible = true },
                new HudStep { Part = HudPart.Objective, Visible = true },
                new WaitStep { Seconds = 0.3f },
            };
            if (playerSprite == null || typing == null || idle == null || standUp == null)
            {
                if (seated != null) Debug.LogWarning("[Chain] Floor_03: у Danny_Seated нет Typing/Idle/StandUp или нет спрайта игрока — вступление без арта");
                var simple = new System.Collections.Generic.List<SequenceStep>
                {
                    new HudStep { AllParts = true, Visible = false },
                    new WaitStep { Seconds = 0.6f },
                    new DialogueStep { Dialogue = start },
                };
                simple.AddRange(hud);
                simple.Add(new ControlsHintStep());
                return simple.ToArray();
            }

            // Арт сидящего выключен вне вступления: в редакторе и при любом сбое сценария не будет двух Дэнни.
            seated.SetActive(false);
            typing.SetActive(true);
            idle.SetActive(false);
            standUp.SetActive(false);
            var steps = new System.Collections.Generic.List<SequenceStep>
            {
                new SetActiveStep { Target = seated, Active = true },
                new SetActiveStep { Target = playerSprite, Active = false },
                new HudStep { AllParts = true, Visible = false },
                new WaitStep { Seconds = 0.6f },
                new ControlsHintStep(),
                new WaitStep { Seconds = 2.5f },                  // печатает
                new SetActiveStep { Target = typing, Active = false },
                new SetActiveStep { Target = idle, Active = true },
                new WaitStep { Seconds = 0.8f },                  // сидит
                new DialogueStep { Dialogue = start },            // «Damn… It's late already.» — почему встаёт из-за компьютера
                new SetActiveStep { Target = idle, Active = false },
                new SetActiveStep { Target = standUp, Active = true },
                new WaitStep { Seconds = 2f },                    // клип Danny_StandUp — 1.76 с
                new SetActiveStep { Target = seated, Active = false },
                new SetActiveStep { Target = playerSprite, Active = true },
            };
            steps.AddRange(hud);
            return steps.ToArray();
        }

        /// <summary>Объект сцены по имени, включая выключенные.</summary>
        static GameObject FindInScene(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        static GameObject Child(GameObject parent, string name)
        {
            if (parent == null) return null;
            var t = parent.transform.Find(name);
            return t != null ? t.gameObject : null;
        }

        /// <summary>Спрайт Дэнни-игрока (Player/Sprite): на время вступления его заменяет арт сидящего.</summary>
        static GameObject FindPlayerSprite()
        {
            foreach (var player in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
            {
                var t = player.transform.Find("Sprite");
                if (t != null) return t.gameObject;
            }
            return null;
        }

        /// <summary>Дверь из коридора в кухню: DoorInteractable с направлением IntoRoom и комнатой Kitchen.</summary>
        static DoorInteractable FindKitchenDoor()
        {
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(door);
                var room = so.FindProperty("room").objectReferenceValue as Component;
                bool intoRoom = so.FindProperty("direction").enumValueIndex == (int)DoorInteractable.Direction.IntoRoom;
                if (intoRoom && room != null && room.name.Contains("Kitchen")) return door;
            }
            return null;
        }

        [MenuItem("Overworked/Build Game Chain/Floor 02 (flashlight, dark)")]
        public static void BuildFloor02() => BuildFlashlightFloor("Floor_02", globalLight: 0.08f);

        [MenuItem("Overworked/Build Game Chain/Floor 01 (flashlight)")]
        public static void BuildFloor01() => BuildFlashlightFloor("Floor_01", globalLight: -1f);

        [MenuItem("Overworked/Build Game Chain/All Floors")]
        public static void BuildAll()
        {
            BuildFloor03();
            BuildFloor02();
            BuildFloor01();
        }

        /// <summary>
        /// Фонарик P4 (FlashlightController) на этаже: с начала этажа уже в руках у Дэнни, выключен, включается на F.
        /// globalLight ≥ 0 — приглушить общий свет сцены (Floor_02 почти без ламп — без этого луч не видно).
        /// </summary>
        static void BuildFlashlightFloor(string sceneName, float globalLight)
        {
            var scene = EditorSceneManager.OpenScene(Scenes + sceneName + ".unity", OpenSceneMode.Single);
            var group = Group(Root("Interactables"));
            var go = new GameObject("Flashlight");
            go.transform.SetParent(group.transform, false);
            var flashlight = go.AddComponent<FlashlightController>();
            // Если на этаже лежит фонарик для подбора (Floor_02, P2: FlashlightPickup), до подбора он недоступен,
            // а подбор включает его — ссылку переподключаем на новый объект (иначе после пересборки она пустая).
            var pickup = FindPickup("FlashlightPickup");
            var so = new SerializedObject(flashlight);
            so.FindProperty("availableOnStart").boolValue = pickup == null;
            so.FindProperty("onAtStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (pickup != null) RelinkSetAvailable(pickup, flashlight);

            if (globalLight >= 0f)
            {
                var global = GameObject.Find("Global Light 2D");
                var light = global != null ? global.GetComponent<UnityEngine.Rendering.Universal.Light2D>() : null;
                if (light != null) light.intensity = globalLight;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Chain] {sceneName}: фонарик в руках" + (globalLight >= 0f ? $", общий свет {globalLight}" : ""));
        }

        static PickupInteractable FindPickup(string name)
        {
            foreach (var p in Object.FindObjectsByType<PickupInteractable>(FindObjectsInactive.Include))
                if (p.name == name) return p;
            return null;
        }

        /// <summary>Вызовы FlashlightController.SetAvailable в onDone подбора: пустую/старую цель меняем на новый фонарик.</summary>
        static void RelinkSetAvailable(PickupInteractable pickup, FlashlightController flashlight)
        {
            var so = new SerializedObject(pickup);
            var calls = so.FindProperty("onDone.m_PersistentCalls.m_Calls");
            bool found = false;
            for (int i = 0; calls != null && i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (call.FindPropertyRelative("m_MethodName").stringValue != "SetAvailable") continue;
                call.FindPropertyRelative("m_Target").objectReferenceValue = flashlight;
                found = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!found)
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(pickup.onDone,
                    new UnityEngine.Events.UnityAction<bool>(flashlight.SetAvailable), true);
        }

        // ------------------------------------------------------------ сцена

        static GameObject Root(string name)
        {
            foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go;
            return new GameObject(name);
        }

        /// <summary>Пустая группа Chain внутри root (старая удаляется — пересборка с нуля).</summary>
        static GameObject Group(GameObject root)
        {
            var old = root.transform.Find(ChainGroup);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(ChainGroup);
            go.transform.SetParent(root.transform, false);
            return go;
        }

        static ScriptedSequence Sequence(GameObject parent, string name, bool playOnStart, bool lockPlayer, params SequenceStep[] steps)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var seq = go.AddComponent<ScriptedSequence>();
            var so = new SerializedObject(seq);
            so.FindProperty("playOnStart").boolValue = playOnStart;
            so.FindProperty("once").boolValue = true;
            so.FindProperty("lockPlayer").boolValue = lockPlayer;
            var list = so.FindProperty("steps");
            list.ClearArray();
            foreach (var step in steps)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).managedReferenceValue = step;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return seq;
        }

        /// <summary>Зона-триггер в коридоре (x, ширина) + сценарий, который она запускает; Дэнни стоит.</summary>
        static void Zone(GameObject parent, string name, float x, float width, ObjectiveData requireCompleted, params SequenceStep[] steps)
        {
            var seq = Sequence(parent, name, playOnStart: false, lockPlayer: true, steps);
            seq.transform.position = new Vector3(x, FloorY + 2f, 0);
            var col = seq.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 5f);
            var trigger = seq.gameObject.AddComponent<SequenceTrigger>();
            Set(trigger, "sequence", seq);
            if (requireCompleted != null) Set(trigger, "requireCompleted", requireCompleted);
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static HallucinationEvent FindHallucination(string name)
        {
            foreach (var h in Object.FindObjectsByType<HallucinationEvent>(FindObjectsInactive.Include))
                if (h.name == name) return h;
            Debug.LogWarning($"[Setup] в сцене нет галлюцинации {name}");
            return null;
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
            if (asset == null) Debug.LogWarning($"[Chain] нет ассета {path}");
            return asset;
        }

        // ------------------------------------------------------------ данные

        /// <summary>Реплики Floor_03 из NARRATIVE_EN, которых ещё нет в Data (таблица docs/systems/Texts.md).</summary>
        static void EnsureFloor03Dialogues()
        {
            EnsureDialogue("F3_OpenSpace",
                "Everyone left ages ago.",
                "As always. Just me, stuck here till midnight.");
            EnsureDialogue("F3_Max01_After", "Fine. At least someone remembered I'm here.");
            EnsureDialogue("F3_Max02_After", "Yeah. Good luck to both of us.");
            AssetDatabase.SaveAssets();
        }

        static void EnsureDialogue(string name, params string[] dannyLines)
        {
            string path = $"{Data}Dialogues/{name}.asset";
            if (File.Exists(path)) return;
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[dannyLines.Length];
            for (int i = 0; i < dannyLines.Length; i++)
                d.Lines[i] = new DialogueLine { Speaker = Speaker.Danny, Text = dannyLines[i] };
            AssetDatabase.CreateAsset(d, path);
        }
    }
}
