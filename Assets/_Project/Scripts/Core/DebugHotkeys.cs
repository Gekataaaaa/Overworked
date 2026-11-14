using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладочные клавиши для всей команды (только редактор и development-билд, добавляет Bootstrap).
    /// F1 — MainMenu, F2/F3/F4 — Floor_03/02/01, F5 — WhiteRoom, F6 — fade туда-обратно,
    /// F7 — панель состояния (этаж, имя, рассудок, флаги) в левом верхнем углу.
    /// </summary>
    public class DebugHotkeys : MonoBehaviour
    {
        bool showState;
        GUIStyle style;

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
            if (kb.f7Key.wasPressedThisFrame) showState = !showState;
        }

        void OnGUI()
        {
            if (!showState || GameManager.Instance == null) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.UpperLeft };
            var s = GameManager.Instance.State;
            string sanity = SanityManager.Instance != null ? $"{SanityManager.Instance.Current:0} → {SanityManager.Instance.Target:0}" : "—";
            string player = PlayerController.Instance != null ? PlayerController.Instance.State.ToString() : "нет";
            GUI.Box(new Rect(10, 10, 420, 150),
                $"Floor: {s.CurrentFloor}\nName: {s.PlayerName}\nSanity: {sanity}\nPlayer: {player}\nFlags: {string.Join(", ", s.Flags)}",
                style);
        }
    }
}
