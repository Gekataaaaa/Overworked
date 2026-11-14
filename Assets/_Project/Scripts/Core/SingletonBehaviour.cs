using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Базовый класс для менеджеров-синглтонов: XxxManager.Instance.
    /// Менеджеры из префаба _Systems живут всю игру (DontDestroyOnLoad ставит Bootstrap на корень).
    /// </summary>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        /// <summary>Текущий экземпляр. null, пока объект не создан.</summary>
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[Core] второй экземпляр {typeof(T).Name} уничтожен");
                Destroy(this);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
