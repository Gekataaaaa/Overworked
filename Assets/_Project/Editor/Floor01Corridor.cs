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
    /// Коридор Floor_01, нарратив сцена 3.0:
    /// главный выход — E → описание «заперто, за стеклом пустота» и реплика Дэнни (F1_MainExit);
    /// дверь 102 — заперта до флага Key102, E → табличка (F1_Door102Locked) и задание F1_FindKey101.
    /// Тексты — ассеты Data/ с именами из таблицы P3 (docs/systems/Texts.md), строки — дословно из NARRATIVE_EN.
    /// Уже существующие ассеты не перезаписываются. Повторный запуск пересоздаёт выход и перенастраивает дверь.
    /// Меню: Overworked → P2 → Floor_01 → Build Floor_01 Corridor.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01Corridor
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string RequestFile = "Temp/p2_floor01_corridor.request";

        const string MainExitPath = "Assets/_Project/Data/Inspectables/F1_MainExit.asset";
        const string Door102Path = "Assets/_Project/Data/Dialogues/F1_Door102Locked.asset";
        const string FindKeyPath = "Assets/_Project/Data/Objectives/F1_FindKey101.asset";

        static Floor01Corridor()
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

        [MenuItem("Overworked/P2/Floor_01/Build Floor_01 Corridor")]
        public static void Build()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            // Сначала сцена, потом ассеты: OpenScene выгружает «неиспользуемые» ассеты, и загруженные
            // до него тексты/задания превращались в null — в сцену сохранялись пустые ссылки.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // --- Тексты (NARRATIVE_EN, сцена 3.0)
            var mainExitText = Dialogue(MainExitPath,
                (Speaker.Description, "The door is locked with an electronic lock. Behind the glass — absolute black emptiness. No street, no streetlights."),
                (Speaker.Danny, "There's nothing out there…"));
            var door102Text = Dialogue(Door102Path,
                (Speaker.Description, "The door is locked. The sign reads: \"BREAK ROOM. KEY IS IN ROOM 101.\""));
            var findKey = Objective(FindKeyPath, "F1_FindKey101", "Find the key in room 101.");

            Transform corridor = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") corridor = root.transform.Find("Corridor");
            if (corridor == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Environment/Corridor");
                return;
            }

            // --- Главный выход: «впереди — стеклянные двери главного выхода», в конце коридора.
            var oldExit = corridor.Find("MainExit");
            if (oldExit != null) Object.DestroyImmediate(oldExit.gameObject);
            float halfWidth = CorridorHalfWidth(corridor);
            var size = new Vector2(2.4f, 2.5f);
            var exit = new GameObject("MainExit");
            exit.transform.SetParent(corridor, false);
            exit.transform.localPosition = new Vector3(halfWidth - 3f, -4.2f + size.y / 2f, 0f);
            var col = exit.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            // За стеклом — чёрная пустота.
            SB.Rect(exit, "Sprite", SB.EnsureSquareSprite(), Vector2.zero, size, new Color(0.02f, 0.02f, 0.03f), -8);
            RoomWiring.Set(exit.AddComponent<DoorInteractable>(),
                ("doorLocked", true), ("lockedPrompt", "Check the exit"), ("lockedDialogue", mainExitText));

            // --- Дверь 102: заперта до ключа из 101, попытка открыть выдаёт задание.
            var door = corridor.Find("Doors/Door_Room_102");
            if (door == null || !door.TryGetComponent(out DoorInteractable door102))
            {
                Debug.LogError("[Setup] Floor_01: у Door_Room_102 нет DoorInteractable — сначала Overworked → P2 → All Floors → Wire Rooms On All Floors");
                return;
            }
            RoomWiring.Set(door102,
                ("doorLocked", true), ("unlockFlag", "Key102"),
                ("lockedPrompt", "Check room 102"), ("lockedDialogue", door102Text),
                ("prompt", "Unlock and enter room 102"));

            // Задание — через ObjectiveGiver на onLockedTried; старые подписки убираем, чтобы не дублировались.
            if (!door.TryGetComponent(out ObjectiveGiver giver)) giver = door.gameObject.AddComponent<ObjectiveGiver>();
            RoomWiring.Set(giver, ("objective", findKey));
            for (int i = door102.onLockedTried.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(door102.onLockedTried, i);
            UnityEventTools.AddPersistentListener(door102.onLockedTried, giver.Give);
            EditorUtility.SetDirty(door102);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_01: главный выход и замок двери 102 готовы");
        }

        // Полуширина коридора — по правой стене каркаса (WallRight стоит на halfWidth + 0.5).
        static float CorridorHalfWidth(Transform corridor)
        {
            var wall = corridor.Find("Walls/WallRight");
            return wall != null ? wall.localPosition.x - 0.5f : 36f;
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
