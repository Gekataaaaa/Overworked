using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Убирает из игры красное мерцание экрана — вспышки ScreenFlashVisual красного цвета («Distortion» в H2, H4
    /// и в копии H4 у Room_201). Чёрные вспышки (мигание света, мигание в такт пульсу) остаются.
    /// Галлюцинация собирает визуалы из детей при запуске, поэтому удалённый объект просто не проигрывается.
    /// Меню: Overworked → P1 → Art → Remove Red Flicker.
    /// </summary>
    public static class RemoveRedFlicker
    {
        const string PrefabDir = "Assets/_Project/Prefabs/Hallucinations";
        static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Floor_03.unity", "Assets/_Project/Scenes/Floor_02.unity",
            "Assets/_Project/Scenes/Floor_01.unity", "Assets/_Project/Scenes/WhiteRoom.unity",
        };

        [MenuItem("Overworked/P1/Art/Remove Red Flicker")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int removed = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = RemoveIn(root);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    removed += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = 0;
                foreach (var go in scene.GetRootGameObjects())
                {
                    // Внутри экземпляра префаба объект не удалить — его уже убрали из самого префаба выше.
                    if (PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                    n += RemoveIn(go);
                }
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                removed += n;
            }
            Debug.Log($"[Setup] Красное мерцание убрано: {removed} вспышек");
        }

        static int RemoveIn(GameObject root)
        {
            int n = 0;
            foreach (var f in root.GetComponentsInChildren<ScreenFlashVisual>(true))
            {
                if (f == null || PrefabUtility.IsPartOfPrefabInstance(f.gameObject)) continue;
                var c = new SerializedObject(f).FindProperty("color").colorValue;
                if (c.r < 0.15f || c.r < (c.g + c.b) * 1.5f) continue;   // не красная (чёрная/серая) — оставляем
                Object.DestroyImmediate(f.gameObject);
                n++;
            }
            return n;
        }
    }
}
