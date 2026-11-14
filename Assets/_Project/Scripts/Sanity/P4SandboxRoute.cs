using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked
{
    /// <summary>
    /// Прохождение по копиям сцен P4 (Scenes/Sandbox/Sandbox_P4_MainMenu, Sandbox_P4_Floor_03/02/01) — только в редакторе.
    /// GameManager грузит этажи по фиксированным именам (Floor_0X, MainMenu), поэтому, пока запущена копия,
    /// загруженный оригинал сразу (под чёрным экраном перехода) подменяется своей копией.
    /// WhiteRoom не подменяется — это и так сцена P4. Объект с этим компонентом лежит в каждой копии.
    /// </summary>
    public class P4SandboxRoute : MonoBehaviour
    {
        const string Folder = "Assets/_Project/Scenes/Sandbox/";
        static readonly string[] Originals =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Floor_03.unity",
            "Assets/_Project/Scenes/Floor_02.unity",
            "Assets/_Project/Scenes/Floor_01.unity",
        };

        static bool active;

        /// <summary>Выключить подмену (тест настоящих сцен после копий в одном запуске).</summary>
        public static void Deactivate() => ResetStatic();

        // Сброс между запусками Play Mode (при выключенной перезагрузке домена статика переживает выход).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            active = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Awake()
        {
            if (active) return;
            active = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[P4 Sandbox] маршрут по копиям включён");
        }

        /// <summary>Путь копии для оригинала (null — сцену не подменяем).</summary>
        public static string CopyOf(string originalPath)
        {
            foreach (var o in Originals)
                if (o == originalPath)
                    return Folder + "Sandbox_P4_" + System.IO.Path.GetFileName(o);
            return null;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#if UNITY_EDITOR
            string copy = CopyOf(scene.path);
            if (copy == null) return;
            // Убираем оригинал до его Start(): иначе запустились бы его сценарии,
            // а синглтоны (игрок, фонарик) копии уничтожились бы как «второй экземпляр».
            foreach (var root in scene.GetRootGameObjects()) DestroyImmediate(root);
            Debug.Log($"[P4 Sandbox] {scene.name} → {copy}");
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(copy, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        }
    }
}
