using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Пауза по Esc: останавливает время и звук, блокирует Дэнни, показывает Resume / Main Menu / Quit.
    /// Префаб Prefabs/Systems/Resources/PauseMenu, Bootstrap кладёт его внутрь _Systems.
    /// Не открывается в меню, во время перехода, пока ввод заблокирован (телефон, мини-игра, катсцена)
    /// и пока поверх этажа загружена additive-сцена (Tiny UFO со своей паузой).
    /// </summary>
    public class PauseMenuUI : SingletonBehaviour<PauseMenuUI>
    {
        [Tooltip("Панель паузы (затемнение + кнопки)")]
        [SerializeField] GameObject panel;

        [Tooltip("Кнопка Resume")]
        [SerializeField] Button resumeButton;

        [Tooltip("Кнопка Main Menu")]
        [SerializeField] Button menuButton;

        [Tooltip("Кнопка Quit")]
        [SerializeField] Button quitButton;

        /// <summary>Игра сейчас на паузе.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Пауза включилась (true) или выключилась (false).</summary>
        public event Action<bool> OnPauseChanged;

        void Start()
        {
            resumeButton.onClick.AddListener(Resume);
            menuButton.onClick.AddListener(ToMainMenu);
            quitButton.onClick.AddListener(Quit);
            panel.SetActive(false);
        }

        void Update()
        {
            if (GameInput.Pause == null || !GameInput.Pause.WasPressedThisFrame()) return;
            if (IsPaused) Resume();
            else if (CanPause()) Pause();
        }

        bool CanPause()
        {
            var player = PlayerController.Instance;
            // Нет игрока — это меню; заблокирован — открыт телефон или мини-игра, у них свой Esc.
            if (player == null || player.State != PlayerState.Normal) return false;
            // Tiny UFO грузится additive-сценой поверх этажа и ловит Esc сам (своя пауза).
            // Проверяем отдельно от блокировки игрока: её может снять кто-то другой посреди мини-игры.
            if (SceneManager.sceneCount > 1) return false;
            return SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsLoading;
        }

        /// <summary>Ставит игру на паузу.</summary>
        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(true);
            panel.SetActive(true);
            resumeButton.Select();
            OnPauseChanged?.Invoke(true);
        }

        /// <summary>Снимает паузу.</summary>
        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(false);
            panel.SetActive(false);
            OnPauseChanged?.Invoke(false);
        }

        void ToMainMenu()
        {
            Resume();
            GameManager.Instance.LoadMainMenu();
        }

        void Quit()
        {
            Debug.Log("[UI] Quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
