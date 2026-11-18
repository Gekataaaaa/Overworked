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
    /// Floor_02: фонарик не в руках с начала этажа, а лежит у входа (решение команды, вместо «уже в руках» из нарратива).
    /// «[E] Pick up the flashlight» → фонарик взлетает над Дэнни → «Looks like I found a flashlight!» (F2_FlashlightFound)
    /// → фонарик становится доступен (FlashlightController.SetAvailable(true)) → над Дэнни подсказка
    /// «[F] Turn the flashlight on / off» (FlashlightHint), пока не включит.
    /// Меню: Overworked → P2 → Floor_02 → Floor_02 Flashlight Pickup.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor02Flashlight
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_02.unity";
        const string RequestFile = "Temp/p2_floor02_flashlight.request";
        const string FoundPath = "Assets/_Project/Data/Dialogues/F2_FlashlightFound.asset";
        const float FloorY = -4.2f;

        static Floor02Flashlight()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
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
            Build();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Floor_02/Floor_02 Flashlight Pickup")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Ассет — после OpenScene (иначе сцена выгрузит его и сохранится пустая ссылка).
            var found = AssetDatabase.LoadAssetAtPath<DialogueData>(FoundPath);
            if (found == null)
            {
                found = ScriptableObject.CreateInstance<DialogueData>();
                found.Lines = new[] { new DialogueLine { Speaker = Speaker.Danny, Text = "Looks like I found a flashlight!" } };
                AssetDatabase.CreateAsset(found, FoundPath);
            }

            FlashlightController flashlight = null;
            PlayerController player = null;
            Transform interactables = null, story = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (flashlight == null) flashlight = root.GetComponentInChildren<FlashlightController>(true);
                if (player == null) player = root.GetComponentInChildren<PlayerController>(true);
                if (root.name == "Interactables") interactables = root.transform;
                if (root.name == "Story") story = root.transform;
            }
            if (flashlight == null || player == null)
            {
                Debug.LogError("[Setup] Floor_02: нет FlashlightController (Build Game Chain P3) или игрока");
                return;
            }
            if (interactables == null) interactables = new GameObject("Interactables").transform;
            if (story == null) story = new GameObject("Story").transform;

            // Фонарика нет в руках, пока не подобран.
            RoomWiring.Set(flashlight, ("availableOnStart", false));

            foreach (var (parent, name) in new[] { (interactables, "FlashlightPickup"), (story, "FlashlightHint") })
            {
                var old = parent.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Подсказка про F — отдельный объект: сам фонарик-предмет после подбора выключается.
            var hintGo = new GameObject("FlashlightHint");
            hintGo.transform.SetParent(story, false);
            var hint = hintGo.AddComponent<FlashlightHint>();

            // Фонарик на полу у входа — чуть правее точки, где Дэнни появляется на этаже.
            var size = new Vector2(0.55f, 0.22f);
            var pickupGo = new GameObject("FlashlightPickup");
            pickupGo.transform.SetParent(interactables, false);
            pickupGo.transform.position = new Vector3(player.transform.position.x + 2.5f, FloorY + size.y / 2f, 0f);
            var col = pickupGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.6f);
            SB.Rect(pickupGo, "Sprite", SB.EnsureSquareSprite(), Vector2.zero, size, new Color(0.25f, 0.27f, 0.3f), -6);
            var pickup = pickupGo.AddComponent<PickupInteractable>();
            RoomWiring.Set(pickup, ("prompt", "Pick up the flashlight"), ("setsFlag", "Flashlight"), ("afterDialogue", found));
            UnityEventTools.AddBoolPersistentListener(pickup.onDone, flashlight.SetAvailable, true);
            UnityEventTools.AddPersistentListener(pickup.onDone, hint.Show);
            EditorUtility.SetDirty(pickup);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_02: фонарик лежит у входа, подбор → реплика и подсказка [F]");
        }
    }
}
