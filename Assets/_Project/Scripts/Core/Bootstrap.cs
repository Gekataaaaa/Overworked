using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Создаёт префаб _Systems (все менеджеры + HUD) один раз до загрузки первой сцены.
    /// Поэтому любую сцену можно запускать прямо из редактора.
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>Путь префаба внутри папки Resources.</summary>
        public const string SystemsPath = "_Systems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateSystems()
        {
            if (GameManager.Instance != null) return;
            var prefab = Resources.Load<GameObject>(SystemsPath);
            if (prefab == null)
            {
                Debug.LogError($"[Core] нет Resources/{SystemsPath}.prefab");
                return;
            }
            var systems = Object.Instantiate(prefab);
            systems.name = SystemsPath;
            Object.DontDestroyOnLoad(systems);
        }
    }
}
