using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Главное меню (сцена MainMenu): Continue / New Game / Quit.
    /// Continue — если этаж спрятан под меню (пауза → Main Menu: возвращает в то же место) или есть чекпоинт
    /// на диске (SaveSystem: этаж с начала в сохранённом состоянии), без пролога и ввода имени. При первом запуске его нет.
    /// New Game — затемнение → пролог-терминал с вводом имени; точка Continue и сохранение на диске стираются.
    /// Лежит на MainMenu Canvas, собирается меню Overworked → P1 → Build Main Menu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Tooltip("Панель с заголовком и кнопками")]
        [SerializeField] GameObject menuPanel;

        [Tooltip("Кнопка Continue — видна, только когда есть сохранение")]
        [SerializeField] Button continueButton;

        [Tooltip("Кнопка New Game — пролог с вводом имени")]
        [FormerlySerializedAs("startButton")]
        [SerializeField] Button newGameButton;

        [Tooltip("Кнопка Quit")]
        [SerializeField] Button quitButton;

        [Tooltip("Пролог-терминал, запускается после New Game")]
        [SerializeField] PrologueTerminal prologue;

        [Tooltip("Секунд на затемнение между меню и терминалом")]
        [SerializeField] float fadeTime = 0.6f;

        bool starting;

        void Start()
        {
            bool canContinue = GameManager.Instance.CanContinueAny && continueButton != null;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(canContinue);
                continueButton.onClick.AddListener(OnContinue);
            }
            newGameButton.onClick.AddListener(OnNewGame);
            quitButton.onClick.AddListener(OnQuit);
            menuPanel.SetActive(true);
            prologue.gameObject.SetActive(false);
            // CRT как у терминала и автомата Tiny UFO — уже на меню.
            prologue.EnableCrt();
            // Выделяем первую кнопку, чтобы меню работало и с клавиатуры (стрелки + Enter).
            (canContinue ? continueButton : newGameButton).Select();
        }

        void OnContinue()
        {
            if (starting) return;
            starting = true;
            Debug.Log("[UI] Continue");
            GameManager.Instance.ContinueFromSave();
        }

        void OnNewGame()
        {
            if (starting) return;
            starting = true;
            GameManager.Instance.ResetState();
            SaveSystem.Delete();
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
