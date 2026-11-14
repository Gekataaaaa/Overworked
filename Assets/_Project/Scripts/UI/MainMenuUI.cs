using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Главное меню (сцена MainMenu): Start → затемнение → пролог-терминал, Quit → выход.
    /// Лежит на MainMenu Canvas, собирается меню Overworked → P1 → Build Main Menu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Tooltip("Панель с заголовком и кнопками")]
        [SerializeField] GameObject menuPanel;

        [Tooltip("Кнопка Start")]
        [SerializeField] Button startButton;

        [Tooltip("Кнопка Quit")]
        [SerializeField] Button quitButton;

        [Tooltip("Пролог-терминал, запускается после Start")]
        [SerializeField] PrologueTerminal prologue;

        [Tooltip("Секунд на затемнение между меню и терминалом")]
        [SerializeField] float fadeTime = 0.6f;

        bool starting;

        void Start()
        {
            startButton.onClick.AddListener(OnStart);
            quitButton.onClick.AddListener(OnQuit);
            menuPanel.SetActive(true);
            prologue.gameObject.SetActive(false);
            // Выделяем Start, чтобы меню работало и с клавиатуры (стрелки + Enter).
            startButton.Select();
        }

        void OnStart()
        {
            if (starting) return;
            starting = true;
            GameManager.Instance.ResetState();
            FadeController.Instance.FadeOut(fadeTime, () =>
            {
                menuPanel.SetActive(false);
                prologue.Begin();
                FadeController.Instance.FadeIn(fadeTime, null);
            });
        }

        void OnQuit()
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
