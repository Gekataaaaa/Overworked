using System;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Заготовки P4: Audio Mixer (Music / Ambient / SFX / Heartbeat) с подключением к AudioManager в _Systems
    /// и тёмный коридор с фонариком в Sandbox_P4. Шкала рассудка и пульс — из HUD P3 в _Systems.
    /// Запуск: меню Overworked → P4 → Sandbox → Build Horror Sandbox,
    /// или batchmode -executeMethod Overworked.EditorTools.P4SandboxBuilder.Run.
    /// Готовые миксер и префабы не перезаписываются; объекты песочницы пересоздаются.
    /// </summary>
    public static class P4SandboxBuilder
    {
        const string MixerPath = "Assets/_Project/Settings/MainMixer.mixer";
        const string SystemsPrefabPath = "Assets/_Project/Prefabs/Systems/Resources/_Systems.prefab";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_P4.unity";

        static readonly string[] Channels = { "Music", "Ambient", "SFX", "Heartbeat" };

        // Геометрия коридора — как в Sandbox_P1 (камера 5.4 → кадр 19.2 × 10.8).
        const float HalfWidth = 30f;
        const float HalfHeight = 5.4f;
        const float FloorY = -4.2f;
        const float BoundsMargin = 0.25f;

        [MenuItem("Overworked/P4/Sandbox/Build Horror Sandbox")]
        public static void Run()
        {
            var mixer = EnsureMixer();
            if (mixer != null) WireAudioManager(mixer);
            var square = PlayerSandboxBuilder.EnsureSquareSprite();
            BuildScene(square);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] P4: миксер и Sandbox_P4 готовы");
        }

        // ------------------------------------------------------------ Audio Mixer

        /// <summary>
        /// Создаёт миксер с группами-каналами. Публичного API для этого у Unity нет, поэтому
        /// через внутренний AudioMixerController (то же делает пункт меню Create → Audio Mixer).
        /// Если в новой версии Unity метод пропадёт — миксер нужно создать руками (см. docs/systems/AudioManager.md).
        /// </summary>
        static AudioMixer EnsureMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null) return existing;

            const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            try
            {
                var type = typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                var create = type?.GetMethod("CreateMixerControllerAtPath", Any);
                var newGroup = type?.GetMethod("CreateNewGroup", Any);
                var addChild = type?.GetMethod("AddChildToParent", Any);
                var masterProp = type?.GetProperty("masterGroup", Any);
                if (create == null || newGroup == null || addChild == null || masterProp == null)
                    throw new MissingMethodException("AudioMixerController API не найден");

                var mixer = create.Invoke(null, new object[] { MixerPath });
                var master = masterProp.GetValue(mixer);
                foreach (var channel in Channels)
                {
                    var group = newGroup.Invoke(mixer, new object[] { channel, false });
                    addChild.Invoke(mixer, new[] { group, master });
                }
                EditorUtility.SetDirty((Object)mixer);
                AssetDatabase.SaveAssets();
                return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Setup] миксер не создан автоматически, создайте Settings/MainMixer руками: {e.Message}");
                return null;
            }
        }

        /// <summary>Назначает группы миксера каналам AudioManager в префабе _Systems (меняются только поля AudioManager).</summary>
        static void WireAudioManager(AudioMixer mixer)
        {
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefabPath);
            try
            {
                var audio = root.GetComponentInChildren<AudioManager>(true);
                if (audio == null) { Debug.LogWarning("[Setup] в _Systems нет AudioManager"); return; }
                var so = new SerializedObject(audio);
                bool changed = false;
                foreach (var channel in Channels)
                {
                    var prop = so.FindProperty(channel.ToLowerInvariant() + "Group");
                    var groups = mixer.FindMatchingGroups($"Master/{channel}");
                    if (prop == null || groups.Length == 0 || prop.objectReferenceValue == groups[0]) continue;
                    prop.objectReferenceValue = groups[0];
                    changed = true;
                }
                if (!changed) return;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------ сцена

        static void BuildScene(Sprite square)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name is "Environment" or "Player" or "Cameras" or "Flashlight" or "P4_TestHUD" or "P4_Debug")
                    Object.DestroyImmediate(go);

            // Почти темнота, как на Floor_02: основной свет — фонарик.
            foreach (var go in scene.GetRootGameObjects())
            {
                var global = go.GetComponent<Light2D>();
                if (global != null && global.lightType == Light2D.LightType.Global) global.intensity = 0.08f;
            }

            var env = new GameObject("Environment");
            float top = HalfHeight + BoundsMargin, bottom = -HalfHeight - BoundsMargin;
            PlayerSandboxBuilder.Rect(env, "BackWall", square, new Vector2(0, (top + FloorY) / 2), new Vector2(HalfWidth * 2, top - FloorY), new Color(0.3f, 0.36f, 0.34f), -10);
            PlayerSandboxBuilder.Rect(env, "Floor", square, new Vector2(0, (FloorY + bottom) / 2), new Vector2(HalfWidth * 2, FloorY - bottom), new Color(0.2f, 0.22f, 0.24f), -5);
            for (int i = -4; i <= 4; i++)
                PlayerSandboxBuilder.Rect(env, $"Door_{i + 5}", square, new Vector2(i * 6f, FloorY + 1.25f), new Vector2(1.4f, 2.5f), new Color(0.42f, 0.4f, 0.32f), -8);
            Wall(env, "WallLeft", square, -HalfWidth - 0.5f);
            Wall(env, "WallRight", square, HalfWidth + 0.5f);

            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(env.transform, false);
            var poly = bounds.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = new[]
            {
                new Vector2(-HalfWidth - 1, bottom), new Vector2(HalfWidth + 1, bottom),
                new Vector2(HalfWidth + 1, top), new Vector2(-HalfWidth - 1, top),
            };

            var player = (GameObject)PrefabUtility.InstantiatePrefab(PlayerSandboxBuilder.EnsurePlayerPrefab(square), scene);
            player.transform.position = new Vector3(0, FloorY + 0.9f, 0);

            var cameras = new GameObject("Cameras");
            var vcam = (GameObject)PrefabUtility.InstantiatePrefab(PlayerSandboxBuilder.EnsureCameraPrefab(), scene);
            vcam.name = "CM Corridor";
            vcam.transform.SetParent(cameras.transform, false);
            vcam.GetComponent<CinemachineConfiner2D>().BoundingShape2D = poly;

            // Фонарик: Spot Light создаётся самим FlashlightController при старте.
            new GameObject("Flashlight").AddComponent<FlashlightController>();

            // Шкала рассудка и пульс — из общего HUD в _Systems (вёрстка P3), своего HUD у песочницы нет.
            new GameObject("P4_Debug").AddComponent<HorrorDebugKeys>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void Wall(GameObject parent, string name, Sprite square, float x)
        {
            var wall = PlayerSandboxBuilder.Rect(parent, name, square, new Vector2(x, 0f), new Vector2(1f, (HalfHeight + BoundsMargin) * 2), new Color(0.15f, 0.16f, 0.15f), -4);
            wall.AddComponent<BoxCollider2D>();
        }
    }
}
