using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Room_101 на Floor_01, нарратив сцена 3.1 — тумба с ящиком и ключ от 102:
    /// «[E] Open the drawer» → описание бейджа и реплики Дэнни (F1_Drawer101) → на тумбе появляется ключ;
    /// «[E] Pick up the key» → ключ взлетает над Дэнни (как пропуск), тост «Received: Room 102 key.»,
    /// флаг Key102 (открывает дверь 102), задание F1_FindKey101 выполнено, выдаётся F1_FinishWork.
    /// Тексты — ассеты Data/ с именами из таблицы P3 (docs/systems/Texts.md), строки дословно из NARRATIVE_EN.
    /// Уже существующие ассеты не перезаписываются. Повторный запуск пересоздаёт тумбу.
    /// Меню: Overworked → P2 → Floor_01 → Build Floor_01 Room 101 Drawer And Key.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01Room101
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string RequestFile = "Temp/p2_room101.request";

        const string DrawerTextPath = "Assets/_Project/Data/Dialogues/F1_Drawer101.asset";
        const string FindKeyPath = "Assets/_Project/Data/Objectives/F1_FindKey101.asset";
        const string FinishWorkPath = "Assets/_Project/Data/Objectives/F1_FinishWork.asset";

        // Справа от двери выхода (она по центру комнаты), чтобы подсказки не перекрывались.
        const float CabinetX = 4f;
        static readonly Vector2 Size = new Vector2(1.2f, 1.2f);

        static Floor01Room101()
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

        [MenuItem("Overworked/P2/Floor_01/Build Floor_01 Room 101 Drawer And Key")]
        public static void Build()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            // Сначала сцена, потом ассеты: OpenScene выгружает «неиспользуемые» ассеты, и загруженные
            // до него тексты/задания превращались в null — в сцену сохранялись пустые ссылки.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // --- Тексты (NARRATIVE_EN, сцена 3.1)
            var drawerText = Dialogue(DrawerTextPath,
                (Speaker.Description, "The badge reads {name}, position: \"Senior Operator\"."),
                (Speaker.Danny, "Senior operator?.."),
                (Speaker.Danny, "No. This isn't funny anymore."));
            var findKey = Objective(FindKeyPath, "F1_FindKey101", "Find the key in room 101.");
            var finishWork = Objective(FinishWorkPath, "F1_FinishWork", "Finish work in room 102.");

            Transform room = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") room = root.transform.Find("Rooms/Room_101");
            if (room == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Environment/Rooms/Room_101");
                return;
            }

            var props = room.Find("Props");
            if (props == null)
            {
                props = new GameObject("Props").transform;
                props.SetParent(room, false);
            }
            foreach (var name in new[] { "Cabinet", "Key102" })
            {
                var old = props.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Пол комнаты — на 0.9 ниже точки входа (ноги Дэнни), тумба стоит на нём.
            var entry = room.Find("EntryPoint");
            float floorY = entry != null ? entry.localPosition.y - 0.9f : -4.2f;

            var cabinet = new GameObject("Cabinet");
            cabinet.transform.SetParent(props, false);
            cabinet.transform.localPosition = new Vector3(CabinetX, floorY + Size.y / 2f, 0f);
            var col = cabinet.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Size;
            SB.Rect(cabinet, "Sprite", SB.EnsureSquareSprite(), Vector2.zero, Size, new Color(0.4f, 0.32f, 0.25f), -7);

            // --- Ключ от 102: лежит на тумбе, виден только после открытия ящика.
            var keySize = new Vector2(0.5f, 0.2f);
            var key = new GameObject("Key102");
            key.transform.SetParent(props, false);
            key.transform.localPosition = new Vector3(CabinetX, floorY + Size.y + keySize.y / 2f + 0.05f, 0f);
            var keyCol = key.AddComponent<BoxCollider2D>();
            keyCol.isTrigger = true;
            // Зона чуть больше самого ключа — его легко «поймать» подсказкой.
            keyCol.size = new Vector2(0.8f, 0.6f);
            SB.Rect(key, "Sprite", SB.EnsureSquareSprite(), Vector2.zero, keySize, new Color(0.85f, 0.7f, 0.3f), -6);
            var pickup = key.AddComponent<PickupInteractable>();
            RoomWiring.Set(pickup,
                ("prompt", "Pick up the key"),
                ("receivedToast", "Received: Room 102 key."),
                ("setsFlag", "Key102"),
                ("completesObjective", findKey));
            // После ключа — новое задание «Finish work in room 102» (нарратив: «A new task appears»).
            var giver = key.AddComponent<ObjectiveGiver>();
            RoomWiring.Set(giver, ("objective", finishWork));
            UnityEventTools.AddPersistentListener(pickup.onDone, giver.Give);
            EditorUtility.SetDirty(pickup);
            key.SetActive(false);

            // --- Ящик: содержимое (бейдж, реплики), затем на тумбе появляется ключ.
            var drawer = cabinet.AddComponent<DrawerInteractable>();
            RoomWiring.Set(drawer,
                ("prompt", "Open the drawer"),
                ("contentsDialogue", drawerText));
            UnityEventTools.AddBoolPersistentListener(drawer.onDone, key.SetActive, true);
            EditorUtility.SetDirty(drawer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_01: тумба и ключ от 102 в Room_101 готовы");
        }

        static DialogueData Dialogue(string path, params (Speaker speaker, string text)[] lines)
        {
            var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (d != null) return d; // уже есть (мог сделать P3) — не трогаем
            d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = lines[i].speaker, Text = lines[i].text };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static ObjectiveData Objective(string path, string id, string title)
        {
            var o = AssetDatabase.LoadAssetAtPath<ObjectiveData>(path);
            if (o != null) return o;
            o = ScriptableObject.CreateInstance<ObjectiveData>();
            o.Id = id;
            o.Title = title;
            AssetDatabase.CreateAsset(o, path);
            return o;
        }
    }
}
