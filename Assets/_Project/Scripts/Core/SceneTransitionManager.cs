using UnityEngine.SceneManagement;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Смена сцен через затемнение. ЗАГЛУШКА: пока грузит сразу, fade добавит P1.
    /// </summary>
    public class SceneTransitionManager : SingletonBehaviour<SceneTransitionManager>
    {
        /// <summary>Загружает сцену по имени (fade out → load → fade in).</summary>
        public void Load(string sceneName)
        {
            Debug.Log($"[Core] Load scene {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
