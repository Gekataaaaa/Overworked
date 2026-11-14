using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Разовая генерация каркаса проекта: папки по паспорту (раздел 31), сцены этажей
    /// по шаблону (раздел 26), префаб _Systems и Build Settings.
    /// Запуск: меню Overworked → Setup Project, или batchmode -executeMethod Overworked.EditorTools.ProjectSetup.Run.
    /// Повторный запуск НЕ перезаписывает существующие сцены и префаб.
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string ScenesDir = Root + "/Scenes";
        const string SystemsPrefab = Root + "/Prefabs/Systems/Resources/_Systems.prefab";
        const string TinyUfoScene = "Assets/Microgames/gc26_spinv/Scenes/gc26_spinv_Main.unity";

        static readonly string[] Folders =
        {
            "Scenes/Sandbox",
            "Prefabs/Systems/Resources", "Prefabs/Player", "Prefabs/Interactables", "Prefabs/Hallucinations",
            "Prefabs/MiniGames", "Prefabs/Environment", "Prefabs/UI",
            "Art/Characters", "Art/Environment", "Art/Props", "Art/UI", "Art/Effects",
            "Animations", "Materials", "Fonts",
            "Audio/Music", "Audio/Ambient", "Audio/SFX", "Audio/Heartbeat", "Audio/Horror", "Audio/UI",
            "Data/Messages", "Data/Objectives", "Data/Dialogues", "Data/Inspectables",
            "Scripts/Interaction", "Scripts/Objectives", "Scripts/MiniGames", "Scripts/Phone", "Scripts/Dialogue",
            "Scripts/Story", "Scripts/Sanity", "Scripts/Heartbeat", "Scripts/Hallucinations", "Scripts/Flashlight",
            "Scripts/Audio", "Scripts/Effects", "Scripts/UI", "Scripts/Core", "Scripts/Player",
        };

        // Сцена, номер этажа, стартовый рассудок, комнаты, есть ли лестница.
        static readonly (string scene, int floor, float sanity, string[] rooms, bool stairs)[] Floors =
        {
            ("Floor_03", 3, 100f, new[] { "Kitchen" }, true),
            ("Floor_02", 2, 90f, new[] { "Room_201" }, true),
            ("Floor_01", 1, 50f, new[] { "Room_101", "Room_102" }, false),
        };

        [MenuItem("Overworked/Setup Project")]
        public static void Run()
        {
            MoveTemplateLeftovers();
            foreach (var f in Folders) EnsureFolder($"{Root}/{f}");
            ConfigureEditor();

            CreateSystemsPrefab();

            CreateScene("MainMenu", s => AddCamera(Color.black));
            foreach (var f in Floors) CreateScene(f.scene, s => BuildFloor(f.floor, f.sanity, f.rooms, f.stairs));
            CreateScene("WhiteRoom", s =>
            {
                AddCamera(Color.white);
                AddFloorSettings(0, 0f);
                new GameObject("Player");
                new GameObject("Story");
            });
            for (int i = 1; i <= 4; i++)
                CreateScene($"Sandbox/Sandbox_P{i}", s => AddCamera(new Color(0.12f, 0.14f, 0.13f)));

            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{ScenesDir}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/Floor_03.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/Floor_02.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/Floor_01.unity", true),
                new EditorBuildSettingsScene($"{ScenesDir}/WhiteRoom.unity", true),
            };
            // Tiny UFO грузится additive-сценой поверх Floor_01 — в билде без неё не запустится.
            if (File.Exists(TinyUfoScene)) buildScenes.Add(new EditorBuildSettingsScene(TinyUfoScene, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddGitKeeps();
            Debug.Log("[Setup] Overworked: каркас проекта готов");
        }

        // ------------------------------------------------------------ настройки

        static void ConfigureEditor()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.productName = "Overworked";
            PlayerSettings.companyName = "Overworked Team";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        /// <summary>Переносит URP-настройки шаблона в _Project/Settings и удаляет SampleScene.</summary>
        static void MoveTemplateLeftovers()
        {
            // Папку целиком: MoveAsset сохраняет GUID, ссылки Graphics/Quality Settings на URP-ассеты не рвутся.
            if (AssetDatabase.IsValidFolder("Assets/Settings") && !AssetDatabase.IsValidFolder($"{Root}/Settings"))
            {
                string err = AssetDatabase.MoveAsset("Assets/Settings", $"{Root}/Settings");
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[Setup] Settings: {err}");
            }
            EnsureFolder($"{Root}/Settings");
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
            DeleteIfEmpty("Assets/Scenes");
        }

        // ------------------------------------------------------------ _Systems

        static void CreateSystemsPrefab()
        {
            if (File.Exists(SystemsPrefab)) return;

            var root = new GameObject("_Systems");
            root.AddComponent<GameManager>();
            root.AddComponent<SceneTransitionManager>();
            root.AddComponent<SanityManager>();
            root.AddComponent<HeartbeatManager>();
            root.AddComponent<PhoneManager>();
            root.AddComponent<MessageManager>();
            root.AddComponent<ObjectiveManager>();
            root.AddComponent<DialogueManager>();
            root.AddComponent<HallucinationManager>();
            root.AddComponent<AudioManager>();
            root.AddComponent<MiniGameHost>();
            root.AddComponent<UIManager>();

            // HUD: один Canvas на всю игру (паспорт, раздел 21). Наполняет P3.
            var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hud.transform.SetParent(root.transform);
            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // FadePanel — последним, чтобы быть поверх всего HUD.
            var fade = new GameObject("FadePanel", typeof(RectTransform), typeof(Image));
            fade.transform.SetParent(hud.transform, false);
            var rt = (RectTransform)fade.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = fade.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            hud.AddComponent<FadeController>();

            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefab);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------ сцены

        static void CreateScene(string name, System.Action<Scene> fill)
        {
            string path = $"{ScenesDir}/{name}.unity";
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            fill(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>Шаблон этажа (паспорт, раздел 26).</summary>
        static void BuildFloor(int floor, float sanity, string[] rooms, bool stairs)
        {
            var cam = AddCamera(new Color(0.06f, 0.08f, 0.07f));

            var env = new GameObject("Environment");
            var corridor = Child(env, "Corridor");
            foreach (var n in new[] { "Background", "Props", "Lights", "Doors" }) Child(corridor, n);
            var roomsGo = Child(env, "Rooms");
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = Child(roomsGo, rooms[i]);
                // Комнаты ниже коридора, чтобы камера коридора их не захватывала.
                room.transform.position = new Vector3(0, -20 * (i + 1), 0);
                foreach (var n in new[] { "Background", "Props", "Lights", "EntryPoint", "ExitDoor" }) Child(room, n);
            }

            new GameObject("Player");

            var cameras = new GameObject("Cameras");
            var vcam = Child(cameras, "CM Corridor");
            vcam.AddComponent<CinemachineCamera>();
            vcam.transform.position = new Vector3(0, 0, -10);

            new GameObject("Interactables");
            new GameObject("Hallucinations");
            new GameObject("Story");
            if (stairs) new GameObject("Staircase");
            AddFloorSettings(floor, sanity);
        }

        static Camera AddCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.AddComponent<UniversalAdditionalCameraData>();
            go.AddComponent<CinemachineBrain>();
            go.AddComponent<AudioListener>();

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            return cam;
        }

        static void AddFloorSettings(int floor, float sanity)
        {
            var fs = new GameObject("FloorSettings").AddComponent<FloorSettings>();
            var so = new SerializedObject(fs);
            so.FindProperty("floor").intValue = floor;
            so.FindProperty("startSanity").floatValue = sanity;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ------------------------------------------------------------ папки

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void DeleteIfEmpty(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets("", new[] { folder }).Length == 0)
                AssetDatabase.DeleteAsset(folder);
        }

        /// <summary>Git не хранит пустые папки — кладём .gitkeep (Unity его не импортирует).</summary>
        static void AddGitKeeps()
        {
            foreach (var f in Folders)
            {
                string dir = $"{Root}/{f}";
                if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
                    File.WriteAllText($"{dir}/.gitkeep", "");
            }
        }
    }
}
