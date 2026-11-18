using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Починка ссылок на тексты и задания в объектах P2 на этажах. Ранние версии инструментов сборки
    /// загружали ассеты до OpenScene, сцена выгружала их, и в поля сохранялся null — у главного выхода,
    /// двери 102, тумбы и ключа в 101, турникета и пропуска, ящика и компьютера в 201 не было текстов.
    /// Проставляет только ПУСТЫЕ поля, объекты не пересоздаёт — подписки сценария P3 (onDenied и т. п.) целы.
    /// Меню: Overworked → P2 → All Floors → Repair Floor Data References.
    /// </summary>
    [InitializeOnLoad]
    public static class FloorDataRepair
    {
        const string RequestFile = "Temp/p2_repair_refs.request";
        const string Data = "Assets/_Project/Data/";

        static FloorDataRepair()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            // В Play Mode сцены не сохранить — ждём выхода в редактор.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            RepairAll();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/All Floors/Repair Floor Data References")]
        public static void RepairAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;
            int fixedCount = 0;

            // --- Floor_03 (сцены 1.2–1.3)
            var scene = Open("Floor_03");
            fixedCount += Fix<TurnstileInteractable>(scene, "Interactables/Turnstile",
                ("deniedDialogue", "Dialogues/F3_TurnstileDenied"), ("grantedDialogue", "Dialogues/F3_TurnstileGranted"));
            fixedCount += Fix<PickupInteractable>(scene, "Environment/Rooms/Kitchen/Props/MaxPass",
                ("afterDialogue", "Dialogues/F3_PassPicked"), ("completesObjective", "Objectives/F3_TakeMaxPass"));
            Save(scene);

            // --- Floor_02 (сцена 2.2)
            scene = Open("Floor_02");
            fixedCount += Fix<DrawerInteractable>(scene, "Environment/Rooms/Room_201/Props/Desk",
                ("documentSheet", "Dialogues/F2_ShiftSchedule"), ("contentsDialogue", "Dialogues/F2_Drawer201"),
                ("afterPauseDialogue", "Dialogues/F2_Drawer201_Name"));
            fixedCount += Fix<DrawerInteractable>(scene, "Environment/Rooms/Room_201/Props/Desk",
                ("completesObjective", "Objectives/F2_CheckDocs201"));
            fixedCount += Fix<SignalTuningMiniGame>(scene, "Environment/Rooms/Room_201/Props/Computer",
                ("screenText", "Dialogues/F2_Monitor201"));
            fixedCount += Fix<ComputerInteractable>(scene, "Environment/Rooms/Room_201/Props/Computer",
                ("completesObjective", "Objectives/F2_CheckComputer201"));
            Save(scene);

            // --- Floor_01 (сцены 3.0–3.1)
            scene = Open("Floor_01");
            fixedCount += Fix<DoorInteractable>(scene, "Environment/Corridor/MainExit",
                ("lockedDialogue", "Inspectables/F1_MainExit"));
            fixedCount += Fix<DoorInteractable>(scene, "Environment/Corridor/Doors/Door_Room_102",
                ("lockedDialogue", "Dialogues/F1_Door102Locked"));
            fixedCount += Fix<ObjectiveGiver>(scene, "Environment/Corridor/Doors/Door_Room_102",
                ("objective", "Objectives/F1_FindKey101"));
            fixedCount += Fix<DrawerInteractable>(scene, "Environment/Rooms/Room_101/Props/Cabinet",
                ("contentsDialogue", "Dialogues/F1_Drawer101"));
            fixedCount += Fix<PickupInteractable>(scene, "Environment/Rooms/Room_101/Props/Key102",
                ("completesObjective", "Objectives/F1_FindKey101"));
            fixedCount += Fix<ObjectiveGiver>(scene, "Environment/Rooms/Room_101/Props/Key102",
                ("objective", "Objectives/F1_FinishWork"));
            Save(scene);

            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Ссылки на тексты и задания починены: {fixedCount} полей на Floor_03/02/01");
        }

        static Scene Open(string floor) =>
            EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{floor}.unity", OpenSceneMode.Single);

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Ставит ассет в поле, только если поле пустое. Ассет грузится уже ПОСЛЕ открытия сцены.
        static int Fix<T>(Scene scene, string path, params (string field, string asset)[] fields) where T : Component
        {
            var go = Find(scene, path);
            if (go == null || !go.TryGetComponent(out T comp))
            {
                Debug.LogWarning($"[Setup] {scene.name}: нет {typeof(T).Name} на {path}");
                return 0;
            }
            var so = new SerializedObject(comp);
            int n = 0;
            foreach (var (field, asset) in fields)
            {
                var p = so.FindProperty(field);
                if (p == null || p.objectReferenceValue != null) continue;
                var value = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Data + asset + ".asset");
                if (value == null)
                {
                    Debug.LogWarning($"[Setup] нет ассета {asset}");
                    continue;
                }
                p.objectReferenceValue = value;
                n++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return n;
        }

        static GameObject Find(Scene scene, string path)
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
    }
}
