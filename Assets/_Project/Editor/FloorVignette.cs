using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Глобальная виньетка на этажах: тёмные края кадра (URP Post-processing → Vignette).
    /// Профиль Settings/Floors_Vignette.asset (один на все этажи — правится в одном месте),
    /// в каждой сцене этажа — объект GlobalVignette с глобальным Volume, у камеры этажа включается
    /// Post Processing. HUD и подсказки (экранные канвасы) виньетка не затемняет.
    /// Меню: Overworked → P2 → Add Global Vignette To Floors.
    /// </summary>
    [InitializeOnLoad]
    public static class FloorVignette
    {
        const string RequestFile = "Temp/p2_vignette.request";
        const string ProfilePath = "Assets/_Project/Settings/Floors_Vignette.asset";
        const string ObjectName = "GlobalVignette";
        static readonly string[] Floors = { "Floor_03", "Floor_02", "Floor_01" };

        static FloorVignette()
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
            AddToFloors();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Add Global Vignette To Floors")]
        public static void AddToFloors()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;
            EnsureProfile();

            foreach (var floor in Floors)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{floor}.unity", OpenSceneMode.Single);
                // Профиль — после OpenScene (иначе сцена выгрузит его и сохранится пустая ссылка).
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);

                GameObject go = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == ObjectName) go = root;
                if (go == null) go = new GameObject(ObjectName);
                if (!go.TryGetComponent(out Volume volume)) volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 0f;
                volume.weight = 1f;
                volume.sharedProfile = profile;

                // Без Post Processing на камере Volume ничего не рисует.
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                        if (cam.CompareTag("MainCamera")) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Глобальная виньетка на Floor_03/02/01 (профиль Settings/Floors_Vignette)");
        }

        // Профиль с одной Vignette; уже существующий не трогаем — его можно подстроить в инспекторе.
        static void EnsureProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null) return;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            var vignette = profile.Add<Vignette>(true);
            vignette.name = "Vignette";
            vignette.color.Override(Color.black);
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.intensity.Override(0.4f);
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);
            AssetDatabase.AddObjectToAsset(vignette, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}
