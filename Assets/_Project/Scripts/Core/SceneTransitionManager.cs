using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked
{
    /// <summary>
    /// Смена сцен через затемнение: блок игрока → fade out → асинхронная загрузка → fade in.
    /// Живёт в _Systems. Этажи грузятся через GameManager.LoadFloor, меню — через LoadMainMenu.
    /// </summary>
    public class SceneTransitionManager : SingletonBehaviour<SceneTransitionManager>
    {
        [Tooltip("Секунд на уход в чёрное")]
        [SerializeField] float fadeOutTime = 0.6f;

        [Tooltip("Секунд на проявление новой сцены")]
        [SerializeField] float fadeInTime = 0.8f;

        [Tooltip("Пауза на чёрном экране после загрузки, секунд")]
        [SerializeField] float holdBlack = 0.2f;

        /// <summary>Идёт переход: новые вызовы Load игнорируются.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Сцена загружена (экран ещё чёрный, сразу после этого начинается fade in).</summary>
        public event Action<string> OnSceneLoaded;

        /// <summary>Загружает сцену по имени (fade out → load → fade in). Повторный вызов во время перехода игнорируется.</summary>
        public void Load(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[Core] Load({sceneName}) проигнорирован: уже идёт переход");
                return;
            }
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            Debug.Log($"[Core] Load scene {sceneName}");
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(true);

            yield return Fade(true, fadeOutTime);

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[Core] сцены {sceneName} нет в Build Settings");
                yield return Fade(false, fadeInTime);
                IsLoading = false;
                yield break;
            }
            while (!op.isDone) yield return null;

            // Ждём кадр: Start() объектов сцены (FloorSettings, камеры) должен отработать до проявления.
            yield return null;
            OnSceneLoaded?.Invoke(sceneName);
            yield return new WaitForSecondsRealtime(holdBlack);

            yield return Fade(false, fadeInTime);
            IsLoading = false;
        }

        static IEnumerator Fade(bool toBlack, float sec)
        {
            var fade = FadeController.Instance;
            if (fade == null) yield break;
            bool done = false;
            if (toBlack) fade.FadeOut(sec, () => done = true);
            else fade.FadeIn(sec, () => done = true);
            while (!done) yield return null;
        }
    }
}
