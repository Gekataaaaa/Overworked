using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Floor_02: лестница на первый этаж заперта, пока не выполнено задание F2_CheckDocs201
    /// («Check the documents in room 201.»). По нарративу (сцена 2.0) Макс пишет: «система не закроет смену
    /// без сверки архива, документы в 201-й» — пока сверка не сделана, уйти с этажа нельзя.
    /// Как дверь 102 на Floor_01: у запертой лестницы есть подсказка, по E — объяснение (F2_StairsLocked,
    /// строки из сообщения Макса и реплика Дэнни из сцены 2.0). Это делает Story/StairsLockedNotice —
    /// запертая «дверь» поверх лестницы (скрипт лестницы P1 не трогаем).
    /// Отпирает лестницу Story/StairsLock (ObjectiveCompletedEvent по Id задания), кто бы задание ни закрыл:
    /// объяснение убирается, лестница открывается. Подсказка — по нарративу «Go down to the first floor».
    /// Дверь в Room_201 недоступна, пока Дэнни не нажал E у лестницы; после этого она открывается
    /// и выдаётся задание F2_CheckDocs201.
    /// Меню: Overworked → P2 → Lock Floor_02 Stairs Until Room 201.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor02StairsLock
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_02.unity";
        const string RequestFile = "Temp/p2_floor02_stairs.request";
        // Лестница открывается после второго задания в 201 — компьютера (первое — документы).
        const string ObjectiveId = "F2_CheckComputer201";
        const string NoticeTextPath = "Assets/_Project/Data/Dialogues/F2_StairsLocked.asset";
        const string Prompt = "Go down to the first floor";

        static Floor02StairsLock()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            // В Play Mode сцену не сохранить — ждём выхода в редактор.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Build();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Lock Floor_02 Stairs Until Room 201")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            StairInteractable stairs = null;
            Transform story = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Staircase") stairs = root.GetComponentInChildren<StairInteractable>(true);
                if (root.name == "Story") story = root.transform;
            }
            if (stairs == null)
            {
                Debug.LogError("[Setup] Floor_02: нет лестницы (Staircase)");
                return;
            }
            if (story == null) story = new GameObject("Story").transform;

            // Ассет — после OpenScene (иначе сцена выгрузит его и сохранится пустая ссылка).
            var noticeText = AssetDatabase.LoadAssetAtPath<DialogueData>(NoticeTextPath);
            if (noticeText == null)
            {
                // Строки — из сцены 2.0: сообщение Макса и реплика Дэнни после задания.
                noticeText = ScriptableObject.CreateInstance<DialogueData>();
                noticeText.Lines = new[]
                {
                    new DialogueLine { Speaker = Speaker.Description, Text = "The exit downstairs is locked." },
                    new DialogueLine { Speaker = Speaker.Description, Text = "The system won't close the shift without an archive check." },
                    new DialogueLine { Speaker = Speaker.Danny, Text = "I'll help Max and finally get out of here." },
                };
                AssetDatabase.CreateAsset(noticeText, NoticeTextPath);
            }

            // Лестница заперта с начала этажа; подсказку и объяснение даёт StairsLockedNotice.
            RoomWiring.Set(stairs, ("startLocked", true), ("prompt", Prompt));
            PrefabUtility.RecordPrefabInstancePropertyModifications(stairs);

            foreach (var name in new[] { "StairsLock", "StairsLockedNotice" })
            {
                var old = story.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Объяснение у запертой лестницы: та же подсказка, по E — почему нельзя уйти.
            var notice = new GameObject("StairsLockedNotice");
            notice.transform.SetParent(story, false);
            notice.transform.position = stairs.transform.position;
            var stairsCol = stairs.GetComponent<Collider2D>() as BoxCollider2D;
            var col = notice.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = stairsCol != null ? stairsCol.size : new Vector2(2f, 3f);
            col.offset = stairsCol != null ? stairsCol.offset : Vector2.zero;
            RoomWiring.Set(notice.AddComponent<DoorInteractable>(),
                ("doorLocked", true), ("lockedPrompt", Prompt), ("lockedDialogue", noticeText));
            // Жёлтый контур и подсказка — на самой лестнице, а не на невидимой заглушке.
            notice.AddComponent<HighlightTarget>().target = stairs.gameObject;

            // Дверь в 201 недоступна (без E и контура), пока Дэнни не попробовал спуститься: у лестницы он
            // узнаёт, что выход закрыт без сверки архива, — тогда дверь открывается и выдаётся задание.
            var door201 = FindDoorInto(scene, "Room_201");
            var noticeDoor = notice.GetComponent<DoorInteractable>();
            if (door201 != null)
            {
                RoomWiring.Set(door201, ("startLocked", true));
                UnityEventTools.AddBoolPersistentListener(noticeDoor.onLockedTried, door201.SetLocked, false);
                EditorUtility.SetDirty(door201);
            }
            else Debug.LogWarning("[Setup] Floor_02: не найдена дверь в Room_201");
            var checkDocs = AssetDatabase.LoadAssetAtPath<ObjectiveData>("Assets/_Project/Data/Objectives/F2_CheckDocs201.asset");
            if (checkDocs != null)
            {
                var giver = notice.AddComponent<ObjectiveGiver>();
                RoomWiring.Set(giver, ("objective", checkDocs));
                UnityEventTools.AddPersistentListener(noticeDoor.onLockedTried, giver.Give);
            }
            EditorUtility.SetDirty(noticeDoor);

            // Замок: задание выполнено → объяснение убрать, лестницу открыть.
            var lockGo = new GameObject("StairsLock");
            lockGo.transform.SetParent(story, false);
            var ev = lockGo.AddComponent<ObjectiveCompletedEvent>();
            RoomWiring.Set(ev, ("objectiveId", ObjectiveId));
            UnityEventTools.AddBoolPersistentListener(ev.onCompleted, notice.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(ev.onCompleted, stairs.SetLocked, false);
            EditorUtility.SetDirty(ev);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Floor_02: лестница заперта до выполнения {ObjectiveId}, у неё объяснение по E");
        }

        // Дверь в коридоре, ведущая в комнату roomName (DoorInteractable с room = Room_X и направлением IntoRoom).
        static DoorInteractable FindDoorInto(Scene scene, string roomName)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var door in root.GetComponentsInChildren<DoorInteractable>(true))
                {
                    var so = new SerializedObject(door);
                    var room = so.FindProperty("room").objectReferenceValue as Component;
                    if (room != null && room.name == roomName
                        && so.FindProperty("direction").enumValueIndex == (int)DoorInteractable.Direction.IntoRoom)
                        return door;
                }
            return null;
        }
    }
}
