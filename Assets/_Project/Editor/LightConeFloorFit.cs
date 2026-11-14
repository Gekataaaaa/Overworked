using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Конусы света от ламп (объекты Light_Cone*) не должны уходить ниже линии пола: верх остаётся
    /// под лампой, спрайт сжимается по высоте так, чтобы низ стоял ровно на полу (там, где ноги Дэнни).
    /// Линия пола — как у каркаса этажей (FloorSkeletonBuilder): Y = -4.2 относительно коридора/комнаты.
    /// Меню: Overworked → P2 → Fit Light Cones To Floor.
    /// </summary>
    [InitializeOnLoad]
    public static class LightConeFloorFit
    {
        const string RequestFile = "Temp/p2_light_cones.request";
        const float FloorY = -4.2f;
        static readonly string[] Floors = { "Floor_03", "Floor_02", "Floor_01" };

        static LightConeFloorFit()
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
            FitAll();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Fit Light Cones To Floor")]
        public static void FitAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;
            int count = 0;

            foreach (var floor in Floors)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{floor}.unity", OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (!sr.name.StartsWith("Light_Cone") || sr.sprite == null) continue;
                        if (Fit(sr)) { changed = true; count++; }
                    }
                }
                if (!changed) continue;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Конусы света подрезаны по линии пола: {count}");
        }

        // Пол — в координатах родителя «Lights» (коридор или комната), как у каркаса.
        static bool Fit(SpriteRenderer sr)
        {
            var t = sr.transform;
            var parent = t.parent;
            float floorWorld = (parent != null ? parent.TransformPoint(0f, FloorY, 0f) : new Vector3(0f, FloorY, 0f)).y;
            var b = sr.bounds;
            if (b.min.y >= floorWorld - 0.01f) return false; // уже не ниже пола

            float top = b.max.y;
            float newHeight = top - floorWorld;
            if (newHeight <= 0.1f) return false;
            float k = newHeight / b.size.y;

            // Сжатие по Y вокруг верха: верх конуса остаётся под лампой.
            var s = t.localScale;
            t.localScale = new Vector3(s.x, s.y * k, s.z);
            float shift = (top - newHeight / 2f) - b.center.y;
            t.position += new Vector3(0f, shift, 0f);
            Debug.Log($"[Setup] {sr.gameObject.scene.name}/{sr.name}: низ {b.min.y:0.00} → {floorWorld:0.00}");
            return true;
        }
    }
}
