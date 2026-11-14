using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладочные клавиши для всей команды: быстрый переход по сценам и проверка затемнения.
    /// Добавляется Bootstrap'ом только в редакторе и development-билде, в релиз не попадает.
    /// F1 — MainMenu, F2 — Floor_03, F3 — Floor_02, F4 — Floor_01, F5 — WhiteRoom, F6 — fade туда-обратно.
    /// </summary>
    public class DebugHotkeys : MonoBehaviour
    {
        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) GameManager.Instance.LoadMainMenu();
            if (kb.f2Key.wasPressedThisFrame) GameManager.Instance.LoadFloor(3);
            if (kb.f3Key.wasPressedThisFrame) GameManager.Instance.LoadFloor(2);
            if (kb.f4Key.wasPressedThisFrame) GameManager.Instance.LoadFloor(1);
            if (kb.f5Key.wasPressedThisFrame) GameManager.Instance.LoadFloor(0);
            if (kb.f6Key.wasPressedThisFrame && FadeController.Instance != null)
                FadeController.Instance.FadeOut(0.6f, () => FadeController.Instance.FadeIn(0.8f, null));
        }
    }
}
