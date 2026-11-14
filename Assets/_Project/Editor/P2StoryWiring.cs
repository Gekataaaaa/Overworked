using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Привязка текстов P3 и галлюцинаций P4 к объектам P2 по нарративу. Сценарии собираются из шагов P3
    /// (ScriptedSequence: сообщение, реплика, галлюцинация, сбой ввода) в группе Story/P2Chain — она
    /// пересоздаётся при каждом запуске, сценарии P3 (Story/Chain) не трогаются, кроме вставки H1 в его
    /// сцену турникета (там стоит TODO для галлюцинации).
    ///
    /// Floor_03 (1.3): после сообщения Босса у турникета — H1 → F3_H1_After.
    /// Floor_02 (2.2): вход в 201 — силуэт работника (перенесён из H4 101, решение команды).
    /// Floor_02 (2.1): середина коридора — H2 → F2_Silhouette → сбой ввода + F2_InputDrift → взгляд в экран + F2_LookAtScreen.
    ///          (2.2): победа на компьютере в 201 — H3 → F2_H3_After → F2_Max_02 → F2_Boss_02 → F2_Boss02_After.
    /// Floor_01 (3.1): вход в 101 — H4 (лампы в такт пульсу; силуэт перенесён в 201) → F1_H4_After; ключ взят — F1_Max_02 → F1_Boss_02.
    ///          (3.2): у двери 102 с ключом — F1_Door102Approach; вход в 102 — F1_Boss_03 → F1_Boss03_After;
    ///          автомат: победа — F1_ArcadeAfter и задание F1_FinishWork выполнено.
    ///          (3.3): финальный скример H5 при выходе из 102 после победы — уже подключён P4 (FinalScreamerStarter).
    /// Меню: Overworked → P2 → Wire Story And Hallucinations.
    /// </summary>
    [InitializeOnLoad]
    public static class P2StoryWiring
    {
        const string RequestFile = "Temp/p2_story.request";
        const string Data = "Assets/_Project/Data/";
        const string Group = "P2Chain";
        const float FloorY = -4.2f;

        static P2StoryWiring()
        {
            // На тик позже остальных запросов P2: пересборка комнат (201) пересоздаёт объекты, к которым мы цепляемся.
            if (File.Exists(RequestFile)) EditorApplication.delayCall += () => EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            WireAll();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Wire Story And Hallucinations")]
        public static void WireAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;
            WireFloor03();
            WireFloor02();
            WireFloor01();
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Тексты P3 и галлюцинации P4 привязаны к объектам P2 на Floor_03/02/01");
        }

        // ------------------------------------------------------------ Floor_03

        static void WireFloor03()
        {
            var scene = Open("Floor_03");
            var h1 = Hallucination(scene, "H1");
            var after = Load<DialogueData>("Dialogues/F3_H1_After");

            // Сценарий P3 «отказ турникета → Босс → Макс»: H1 — сразу после сообщения Босса (нарратив 1.3:
            // «After the phone is closed, the first hallucination begins»), затем реплики Дэнни.
            ScriptedSequence boss = null;
            foreach (var s in Object.FindObjectsByType<ScriptedSequence>(FindObjectsInactive.Include))
                if (s.name.Contains("DeniedBoss")) boss = s;
            if (boss == null || h1 == null)
            {
                Debug.LogWarning($"[Setup] Floor_03: нет сценария P3 у турникета ({boss != null}) или H1 ({h1 != null})");
            }
            else
            {
                var so = new SerializedObject(boss);
                var steps = so.FindProperty("steps");
                bool has = false;
                for (int i = 0; i < steps.arraySize; i++)
                    if (steps.GetArrayElementAtIndex(i).managedReferenceValue is HallucinationStep) has = true;
                if (!has)
                {
                    // Индекс 1 — после первого шага (сообщение Босса).
                    steps.InsertArrayElementAtIndex(1);
                    steps.GetArrayElementAtIndex(1).managedReferenceValue = new HallucinationStep { Hallucination = h1, Wait = true };
                    steps.InsertArrayElementAtIndex(2);
                    steps.GetArrayElementAtIndex(2).managedReferenceValue = new DialogueStep { Dialogue = after };
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            Save(scene);
        }

        // ------------------------------------------------------------ Floor_02

        static void WireFloor02()
        {
            var scene = Open("Floor_02");
            var group = NewGroup(scene);
            var h2 = Hallucination(scene, "H2");
            var h3 = Hallucination(scene, "H3");

            // 2.1 Середина коридора: силуэт, сбой ввода, Дэнни смотрит в экран.
            // Без блокировки игрока: во время сбоя Дэнни сам «идёт дальше» после отпускания клавиши.
            var silhouette = Sequence(group, "2.1 Silhouette", false,
                new HallucinationStep { Hallucination = h2, Wait = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_Silhouette") },
                new InputDriftStep { Seconds = 3f, Wait = false },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_InputDrift") },
                new PlayAnimStep { Anim = "LookAtScreen" },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_LookAtScreen") });
            Zone(silhouette, CorridorMiddleX(scene), 2f);

            // 2.2 Вход в 201: силуэт работника справа (перенесён из H4 Room_101 — MoveH4Silhouette).
            // Без блокировки: удар — когда Дэнни подходит к силуэту или светит на него.
            HallucinationEvent h4Silhouette = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var h in root.GetComponentsInChildren<HallucinationEvent>(true))
                    if (h.name == MoveH4Silhouette.Floor02Name) h4Silhouette = h;
            if (h4Silhouette != null)
            {
                var enter201 = Sequence(group, "2.2 Room201_Silhouette", false,
                    new HallucinationStep { Hallucination = h4Silhouette, Wait = true });
                var room201 = Find<Room>(scene, "Environment/Rooms/Room_201");
                if (room201 != null) Hook(room201.onEnter, enter201, room201);
            }

            // 2.2 Компьютер в 201: победа в мини-игре 3 → H3 и сообщения.
            var afterComputer = Sequence(group, "2.2 AfterComputer", true,
                new HallucinationStep { Hallucination = h3, Wait = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_H3_After") },
                new MessageStep { Message = Load<MessageData>("Messages/F2_Max_02"), WaitUntilRead = true },
                new MessageStep { Message = Load<MessageData>("Messages/F2_Boss_02"), WaitUntilRead = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_Boss02_After") });
            var computer = Find<ComputerInteractable>(scene, "Environment/Rooms/Room_201/Props/Computer");
            if (computer != null) Hook(computer.onDone, afterComputer, computer);

            Save(scene);
        }

        // ------------------------------------------------------------ Floor_01

        static void WireFloor01()
        {
            var scene = Open("Floor_01");
            var group = NewGroup(scene);
            var h4 = Hallucination(scene, "H4");

            // 3.1 Вход в 101: H4 (силуэт в углу, лампы в такт пульсу) и «Stop it…».
            // Без блокировки: удар — когда Дэнни подходит к силуэту.
            var h4Seq = Sequence(group, "3.1 Room101_H4", false,
                new HallucinationStep { Hallucination = h4, Wait = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F1_H4_After") });
            var room101 = Find<Room>(scene, "Environment/Rooms/Room_101");
            if (room101 != null) Hook(room101.onEnter, h4Seq, room101);

            // 3.1 Ключ взят: телефон — Макс (глитч), следом Босс.
            var afterKey = Sequence(group, "3.1 AfterKey", true,
                new MessageStep { Message = Load<MessageData>("Messages/F1_Max_02"), WaitUntilRead = true },
                new MessageStep { Message = Load<MessageData>("Messages/F1_Boss_02"), WaitUntilRead = true });
            var key = Find<PickupInteractable>(scene, "Environment/Rooms/Room_101/Props/Key102");
            if (key != null) Hook(key.onDone, afterKey, key);

            // 3.2 Подход к двери 102 с ключом: «The break room? Now?»
            var approach = Sequence(group, "3.2 Door102Approach", false,
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F1_Door102Approach") });
            var door102 = FindGo(scene, "Environment/Corridor/Doors/Door_Room_102");
            if (door102 != null)
                Zone(approach, door102.transform.position.x, 3f, Load<ObjectiveData>("Objectives/F1_FindKey101"));

            // 3.2 Вход в 102: сообщение Босса и ответ Дэнни.
            var breakRoom = Sequence(group, "3.2 Room102_Boss", true,
                new MessageStep { Message = Load<MessageData>("Messages/F1_Boss_03"), WaitUntilRead = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F1_Boss03_After") });
            var room102 = Find<Room>(scene, "Environment/Rooms/Room_102");
            if (room102 != null) Hook(room102.onEnter, breakRoom, room102);

            // 3.2 Автомат: победа — реплики «That's it?…» и задание F1_FinishWork выполнено.
            // Объект не пересоздаём: на его onDone подписан финальный скример P4.
            foreach (var arcade in Object.FindObjectsByType<ArcadeInteractable>(FindObjectsInactive.Include))
                RoomWiring.Set(arcade,
                    ("winDialogue", Load<DialogueData>("Dialogues/F1_ArcadeAfter")),
                    ("completesObjective", Load<ObjectiveData>("Objectives/F1_FinishWork")));

            Save(scene);
        }

        // ------------------------------------------------------------ помощники

        static Scene Open(string floor) =>
            EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{floor}.unity", OpenSceneMode.Single);

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Группа Story/P2Chain — пересоздаётся с нуля.
        static GameObject NewGroup(Scene scene)
        {
            GameObject story = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Story") story = root;
            if (story == null) story = new GameObject("Story");
            var old = story.transform.Find(Group);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(Group);
            go.transform.SetParent(story.transform, false);
            return go;
        }

        static ScriptedSequence Sequence(GameObject parent, string name, bool lockPlayer, params SequenceStep[] steps)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var seq = go.AddComponent<ScriptedSequence>();
            var so = new SerializedObject(seq);
            so.FindProperty("playOnStart").boolValue = false;
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

        // Зона-триггер в коридоре на уровне пола; requireCompleted — только после задания.
        static void Zone(ScriptedSequence seq, float x, float width, ObjectiveData requireCompleted = null)
        {
            seq.transform.position = new Vector3(x, FloorY + 2f, 0f);
            var col = seq.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 5f);
            var trigger = seq.gameObject.AddComponent<SequenceTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("sequence").objectReferenceValue = seq;
            if (requireCompleted != null) so.FindProperty("requireCompleted").objectReferenceValue = requireCompleted;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Подписка «событие объекта → сценарий»; старые подписки на сценарии (и битые) убираем — без дублей.
        static void Hook(UnityEvent ev, ScriptedSequence seq, Object owner)
        {
            for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                var target = ev.GetPersistentTarget(i);
                if (target == null || target is ScriptedSequence) UnityEventTools.RemovePersistentListener(ev, i);
            }
            UnityEventTools.AddVoidPersistentListener(ev, new UnityAction(seq.Play));
            EditorUtility.SetDirty(owner);
        }

        // Галлюцинация P4 из папки Hallucinations сцены: H1 / H2 / … по началу имени.
        static HallucinationEvent Hallucination(Scene scene, string prefix)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var h in root.GetComponentsInChildren<HallucinationEvent>(true))
                    if (h.name.StartsWith(prefix)) return h;
            Debug.LogWarning($"[Setup] {scene.name}: нет галлюцинации {prefix} (Overworked → P4 → Add Hallucinations to Floors)");
            return null;
        }

        // Середина коридора — между стенами каркаса.
        static float CorridorMiddleX(Scene scene)
        {
            var left = FindGo(scene, "Environment/Corridor/Walls/WallLeft");
            var right = FindGo(scene, "Environment/Corridor/Walls/WallRight");
            return left != null && right != null ? (left.transform.position.x + right.transform.position.x) / 2f : 0f;
        }

        static T Find<T>(Scene scene, string path) where T : Component
        {
            var go = FindGo(scene, path);
            if (go != null && go.TryGetComponent(out T c)) return c;
            Debug.LogWarning($"[Setup] {scene.name}: нет {typeof(T).Name} на {path}");
            return null;
        }

        static GameObject FindGo(Scene scene, string path)
        {
            var parts = path.Split('/');
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                var t = root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                if (t != null) return t.gameObject;
            }
            return null;
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
            if (asset == null) Debug.LogWarning($"[Setup] нет ассета {path}");
            return asset;
        }
    }
}
