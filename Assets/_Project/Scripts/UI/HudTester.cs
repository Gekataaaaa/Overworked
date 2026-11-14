using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладка HUD в Sandbox_P3: N — записка на весь экран, H — подсказка управления,
    /// J — спрятать/показать весь HUD, K — тост, O — онбординг HUD Floor_03.
    /// В консоль пишется, когда окно закрыто. В сцены этажей не класть.
    /// </summary>
    public class HudTester : MonoBehaviour
    {
        [Tooltip("Текст тестовой записки")]
        [TextArea(2, 6)]
        [SerializeField] string noteText = "THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!";

        [Tooltip("Текст тестового тоста")]
        [SerializeField] string toastText = "Received: Charles's note.";

        [Tooltip("Онбординг HUD для клавиши O")]
        [SerializeField] HudOnboarding onboarding;

        bool hudVisible = true;

        void Update()
        {
            var kb = Keyboard.current;
            var ui = UIManager.Instance;
            if (kb == null || ui == null) return;

            if (kb[Key.N].wasPressedThisFrame)
                ui.ShowFullscreenText(noteText, () => Debug.Log("[UI] записка закрыта"));
            if (kb[Key.H].wasPressedThisFrame)
                ui.ShowControlsHint(() => Debug.Log("[UI] подсказка управления закрыта"));
            if (kb[Key.J].wasPressedThisFrame)
            {
                hudVisible = !hudVisible;
                ui.SetHudVisible(hudVisible);
            }
            if (kb[Key.K].wasPressedThisFrame) ui.ShowToast(toastText);
            if (kb[Key.O].wasPressedThisFrame && onboarding != null)
                onboarding.Play(() => Debug.Log("[Story] онбординг HUD закончен"));
        }
    }
}
