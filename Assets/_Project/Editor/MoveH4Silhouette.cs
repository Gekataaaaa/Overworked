using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Силуэт работника из Галлюцинации 4 (P4, справа от Дэнни в Room_101) переезжает в Room_201 на Floor_02
    /// (решение команды). В Floor_01 у H4 остаются лампы в такт пульсу и рассудок → 25 (силуэт и искажение убраны);
    /// в Floor_02 — копия H4 «H4_Silhouette201» только с силуэтом и искажением, рассудок не трогает
    /// (70, как после H2; дальше H3 → 50). Запускает её вход в 201 (P2StoryWiring).
    /// Префаб H4 не меняется — экземпляры в сценах распаковываются.
    /// Меню: Overworked → P2 → Floor_02 → Move H4 Silhouette To Room 201.
    /// </summary>
    [InitializeOnLoad]
    public static class MoveH4Silhouette
    {
        const string RequestFile = "Temp/p2_move_h4.request";
        const string PrefabPath = "Assets/_Project/Prefabs/Hallucinations/H4_Room101.prefab";
        /// <summary>Имя копии в Floor_02 — по нему её находит P2StoryWiring.</summary>
        public const string Floor02Name = "H4_Silhouette201";

        static MoveH4Silhouette()
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
            Move();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Floor_02/Move H4 Silhouette To Room 201")]
        public static void Move()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            // --- Floor_01: из H4 убрать силуэт и искажение (лампы и рассудок остаются).
            var scene = Open("Floor_01");
            HallucinationEvent h4 = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var h in root.GetComponentsInChildren<HallucinationEvent>(true))
                    if (h.name.StartsWith("H4")) h4 = h;
            if (h4 != null)
            {
                Unpack(h4.gameObject);
                Remove(h4.transform, "Silhouette", "Distortion");
                EditorUtility.SetDirty(h4.gameObject);
            }
            else Debug.LogWarning("[Setup] Floor_01: нет H4");
            Save(scene);

            // --- Floor_02: копия H4 только с силуэтом и искажением.
            scene = Open("Floor_02");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform parent = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Hallucinations") parent = root.transform;
            if (parent == null) parent = new GameObject("Hallucinations").transform;
            var old = parent.Find(Floor02Name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (prefab != null)
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                copy.transform.SetParent(parent, false);
                copy.name = Floor02Name;
                Unpack(copy);
                Remove(copy.transform, "BeatFlicker", "BeatLights");
                // Рассудок не падает сверх H2 (70): дальше по этажу H3 → 50.
                copy.GetComponent<HallucinationEvent>().SanityAfter = 70f;
                EditorUtility.SetDirty(copy);
            }
            else Debug.LogError("[Setup] нет префаба " + PrefabPath);
            Save(scene);

            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Силуэт H4 перенесён из Room_101 в Room_201");
        }

        static void Unpack(GameObject go)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(go),
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        static void Remove(Transform parent, params string[] names)
        {
            foreach (var n in names)
            {
                var t = parent.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        static Scene Open(string floor) =>
            EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{floor}.unity", OpenSceneMode.Single);

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
