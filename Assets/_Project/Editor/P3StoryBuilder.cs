using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: недостающие по нарративу сценарии на Floor_02 и Floor_01 — в том же стиле, что цепочки P1/P2
    /// (сценарий + зона в коридоре, Дэнни стоит, сообщение ждёт прочтения). Кладёт всё в группу Story/P3Chain;
    /// повторный запуск пересобирает только её, расстановку P2 (Story/P2Chain) и P1 (Story/Chain) не трогает.
    /// Floor_02: 2.0 «So dark…» на старте; 2.0 сообщения Макса и Босса на первом включении фонарика;
    /// 2.2 запрет возврата после компьютера в 201. Floor_01: 3.0 сообщения Макса и Босса в коридоре,
    /// Дэнни смотрит в экран. Меню Overworked → P3 → Build Story Floor 02 / Build Story Floor 01.
    /// </summary>
    public static class P3StoryBuilder
    {
        const string Scenes = "Assets/_Project/Scenes/";
        const string Data = "Assets/_Project/Data/";
        const string GroupName = "P3Chain";
        // Линия пола этажей (FloorSkeletonBuilder / ChainBuilder).
        const float FloorY = -4.2f;

        [MenuItem("Overworked/P3/Build Story Floor 02")]
        public static void BuildFloor02()
        {
            // Сцену — ДО загрузки ассетов: OpenScene(Single) выгружает загруженные раньше ассеты.
            var scene = EditorSceneManager.OpenScene(Scenes + "Floor_02.unity", OpenSceneMode.Single);
            var group = NewGroup(scene);

            // 2.0 Тёмная площадка: Дэнни сразу говорит, что темно (дальше — подсказка фонарика P2).
            Sequence(group, "2.0 Start", playOnStart: true, lockPlayer: true,
                new WaitStep { Seconds = 0.6f },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_Start") });

            // 2.0 Первое включение фонарика: Макс, следом Босс (задание F2_CheckDocs201 даёт не он, а лестница — P2),
            // после прочтения — реплика Дэнни.
            var flash = Sequence(group, "2.0 FlashlightMessages", playOnStart: false, lockPlayer: true,
                new MessageStep { Message = Load<MessageData>("Messages/F2_Max_01"), WaitUntilRead = false },
                new MessageStep { Message = Load<MessageData>("Messages/F2_Boss_01"), WaitUntilRead = true },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F2_Boss01_After") });
            flash.gameObject.AddComponent<FlashlightSequenceTrigger>();

            // 2.2 Запрет возврата: после компьютера в 201 игрок выходит в коридор; шаг назад (влево от двери 201) —
            // «Do you still want Danny to keep working here?» + «I just want to go home.», его отводит вправо.
            var door201 = FindX(scene, "Door_Room_201", 4f);
            var noReturn = new GameObject("2.2 NoReturn");
            noReturn.transform.SetParent(group.transform, false);
            noReturn.transform.position = new Vector3(door201 - 2.5f, FloorY + 2f, 0f);
            var col = noReturn.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 5f);
            var nr = noReturn.AddComponent<NoReturnTrigger>();
            var so = new SerializedObject(nr);
            so.FindProperty("dialogue").objectReferenceValue = Load<DialogueData>("Dialogues/F2_NoReturn");
            so.FindProperty("activeAfter").objectReferenceValue = Load<ObjectiveData>("Objectives/F2_CheckComputer201");
            so.FindProperty("pushDirection").intValue = 1;
            so.FindProperty("pushDistance").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, "Floor_02: старт, сообщения на фонарик, запрет возврата");
        }

        [MenuItem("Overworked/P3/Build Story Floor 01")]
        public static void BuildFloor01()
        {
            var scene = EditorSceneManager.OpenScene(Scenes + "Floor_01.unity", OpenSceneMode.Single);
            var group = NewGroup(scene);

            // 3.0 Коридор: игрок идёт от лестницы вправо, проходит дверь 102 (задание F1_FindKey101) — через несколько
            // шагов глитч Макса, следом Босс; Дэнни поворачивается к экрану и говорит с игроком.
            // До комнаты 101; после ключа (задание выполнено) не срабатывает.
            var door102 = FindX(scene, "Door_Room_102", -6f);
            var door101 = FindX(scene, "Door_Room_101", 14f);
            float x = Mathf.Lerp(door102, door101, 0.35f);
            var seq = Zone(group, "3.0 CorridorMessages", x, 1.5f,
                new MessageStep { Message = Load<MessageData>("Messages/F1_Max_01"), WaitUntilRead = false },
                new MessageStep { Message = Load<MessageData>("Messages/F1_Boss_01"), WaitUntilRead = true },
                new PlayAnimStep { Anim = "LookAtScreen", WaitSeconds = 0.6f },
                new DialogueStep { Dialogue = Load<DialogueData>("Dialogues/F1_DannyToPlayer") });
            var trigger = seq.GetComponent<SequenceTrigger>();
            var tso = new SerializedObject(trigger);
            tso.FindProperty("blockIfCompleted").objectReferenceValue = Load<ObjectiveData>("Objectives/F1_FindKey101");
            tso.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, "Floor_01: сообщения Макса и Босса в коридоре, Дэнни смотрит в экран");
        }

        /// <summary>Оба этажа за один запуск (batchmode).</summary>
        public static void BuildAll()
        {
            BuildFloor02();
            BuildFloor01();
        }

        // ------------------------------------------------------------ помощники (как в ChainBuilder)

        static GameObject NewGroup(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject story = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Story") story = go;
            if (story == null) story = new GameObject("Story");
            var old = story.transform.Find(GroupName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var group = new GameObject(GroupName);
            group.transform.SetParent(story.transform, false);
            return group;
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
        static ScriptedSequence Zone(GameObject parent, string name, float x, float width, params SequenceStep[] steps)
        {
            var seq = Sequence(parent, name, playOnStart: false, lockPlayer: true, steps);
            seq.transform.position = new Vector3(x, FloorY + 2f, 0);
            var col = seq.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 5f);
            var trigger = seq.gameObject.AddComponent<SequenceTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("sequence").objectReferenceValue = seq;
            so.ApplyModifiedPropertiesWithoutUndo();
            return seq;
        }

        static float FindX(UnityEngine.SceneManagement.Scene scene, string name, float fallback)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                var t = FindDeep(go.transform, name);
                if (t != null) return t.position.x;
            }
            Debug.LogWarning($"[Setup] нет {name} — беру x = {fallback}");
            return fallback;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(Data + path + ".asset");
            if (a == null) Debug.LogWarning($"[Setup] нет ассета {path}");
            return a;
        }

        static void Save(UnityEngine.SceneManagement.Scene scene, string what)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Setup] {what} (Story/{GroupName})");
        }
    }
}
