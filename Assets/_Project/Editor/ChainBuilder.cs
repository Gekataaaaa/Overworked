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
    /// Сейчас — Floor_03 из механик P3: стартовые реплики, онбординг HUD, подсказка управления,
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
            var square = SB.EnsureSquareSprite();

            // Ассеты грузим после OpenScene: открытие сцены выгружает загруженное раньше (см. MainMenuBuilder).
            var start = Load<DialogueData>("Dialogues/F3_Start");
            var openSpace = Load<DialogueData>("Dialogues/F3_OpenSpace");
            var max01After = Load<DialogueData>("Dialogues/F3_Max01_After");
            var max02After = Load<DialogueData>("Dialogues/F3_Max02_After");
            var charlesDesk = Load<DialogueData>("Inspectables/F3_CharlesDesk");
            var max01 = Load<MessageData>("Messages/F3_Max_01");
            var max02 = Load<MessageData>("Messages/F3_Max_02");
            var boss01 = Load<MessageData>("Messages/F3_Boss_01");
            var takePass = Load<ObjectiveData>("Objectives/F3_TakeMaxPass");

            // 1.0 Начало: Дэнни стоит, реплики, HUD появляется по частям, подсказка управления.
            // TODO(P3): перед репликами — мини-игра 1 «Печать отчёта», когда будет готова.
            var opening = Sequence(story, "1.0 Opening", playOnStart: true, lockPlayer: true,
                new HudStep { AllParts = true, Visible = false },
                new WaitStep { Seconds = 0.6f },
                new DialogueStep { Dialogue = start },
                new HudStep { Part = HudPart.Sanity, Visible = true },
                new HudStep { Part = HudPart.Heartbeat, Visible = true },
                new WaitStep { Seconds = 0.5f },
                new HudStep { Part = HudPart.Phone, Visible = true },
                new HudStep { Part = HudPart.Objective, Visible = true },
                new WaitStep { Seconds = 0.3f },
                new ControlsHintStep());

            // 1.1 Опенспейс — реплики при первом проходе.
            Zone(story, "1.1 OpenSpace", -15f, 2f, null,
                new DialogueStep { Dialogue = openSpace });

            // 1.1 Стол Чарльза — описание при подходе (Inspectable). Записка по E — P2 (NoteInteractable).
            var desk = SB.Rect(props, "CharlesDesk", square, new Vector2(-9f, FloorY + 0.55f), new Vector2(2.4f, 1.1f),
                new Color(0.42f, 0.36f, 0.27f), 2);
            var deskZone = new GameObject("CharlesDesk_Inspect");
            deskZone.transform.SetParent(desk.transform.parent, false);
            deskZone.transform.position = new Vector3(-9f, FloorY + 1.5f, 0);
            var deskCol = deskZone.AddComponent<BoxCollider2D>();
            deskCol.isTrigger = true;
            deskCol.size = new Vector2(3.5f, 4f);
            var inspect = deskZone.AddComponent<Inspectable>();
            Set(inspect, "dialogue", charlesDesk);

            // 1.1 Выход в коридор — сообщение Макса (выдаёт задание «Get Max's pass»), ответ Дэнни.
            Zone(story, "1.1 MaxCallsPass", -2f, 2f, null,
                new MessageStep { Message = max01, WaitUntilRead = true },
                new DialogueStep { Dialogue = max01After });

            // 1.3 Выход из кухни после пропуска — второе сообщение Макса.
            // Сработает, когда P2 положит пропуск (Pickup с completesObjective = F3_TakeMaxPass).
            Zone(story, "1.3 MaxThanks", 9f, 3f, takePass,
                new MessageStep { Message = max02, WaitUntilRead = true },
                new DialogueStep { Dialogue = max02After });

            // 1.3 Турникет P2: своя карта → ACCESS DENIED → onDenied → сообщение Босса (нарратив 1.3).
            // TODO(P4): после него — Галлюцинация 1 (шаг Hallucination).
            var boss = Sequence(story, "1.3 BossAfterDenied", playOnStart: false, lockPlayer: true,
                new MessageStep { Message = boss01, WaitUntilRead = true });
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
            var so = new SerializedObject(flashlight);
            so.FindProperty("availableOnStart").boolValue = true;
            so.FindProperty("onAtStart").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

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
