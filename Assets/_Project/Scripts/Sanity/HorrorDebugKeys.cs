using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладка систем P4 (кладётся объектом в Sandbox_P4; работает только в редакторе и dev-билде).
    /// 1–5 — рассудок 90/70/50/25/0, 0 — сразу 100, H — спайк пульса, S — скример (пульс 160–180, потом плавно спадает),
    /// B — WhiteRoom по шагам (вход: минимальный пульс → «лёг на кровать»: затухание до 0 → выкл),
    /// L — доступность фонарика, Shift+1…5 — галлюцинации H1–H5 сцены (F1–F5 заняты отладкой P1), U — вернуть управление после финального скримера.
    /// </summary>
    public class HorrorDebugKeys : MonoBehaviour
    {
        [Tooltip("Показывать подсказку по клавишам в левом нижнем углу")]
        [SerializeField] bool showHelp = true;

        static readonly float[] SanitySteps = { 90f, 70f, 50f, 25f, 0f };
        int whiteRoomStep;
        GUIStyle style;

        void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            var sanity = SanityManager.Instance;
            var heart = HeartbeatManager.Instance;

            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
            for (int i = 0; i < keys.Length; i++)
            {
                if (!kb[keys[i]].wasPressedThisFrame) continue;
                if (shift) PlayHallucination(i + 1);
                else if (sanity != null) sanity.SetSanity(SanitySteps[i]);
            }
            if (kb.digit0Key.wasPressedThisFrame && sanity != null) sanity.SetSanityInstant(100f);

            if (heart != null)
            {
                if (kb.hKey.wasPressedThisFrame) heart.Spike(40f, 3f);
                if (kb.sKey.wasPressedThisFrame) heart.PlayScreamer();
                if (kb.bKey.wasPressedThisFrame)
                {
                    whiteRoomStep = (whiteRoomStep + 1) % 3;
                    if (whiteRoomStep == 1) heart.EnterWhiteRoom();
                    else if (whiteRoomStep == 2) heart.LieDownInBed();
                    else heart.SetScripted(false);
                }
            }

            var light = FlashlightController.Instance;
            if (kb.lKey.wasPressedThisFrame && light != null) light.SetAvailable(!light.Available);

            // Финальный скример оставляет управление заблокированным — U возвращает его в песочнице.
            if (kb.uKey.wasPressedThisFrame && PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(false);
        }

        /// <summary>Галлюцинация сцены с именем «H{number}_…» (H1_Turnstile … H5_FinalScreamer).</summary>
        static void PlayHallucination(int number)
        {
            foreach (var h in FindObjectsByType<HallucinationEvent>(FindObjectsInactive.Exclude))
            {
                if (!h.name.StartsWith($"H{number}_")) continue;
                if (HallucinationManager.Instance != null) HallucinationManager.Instance.Play(h);
                else h.Play();
                return;
            }
            Debug.Log($"[Hallucinations] в сцене нет H{number}_…");
        }

        void OnGUI()
        {
            if (!showHelp) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft };
            var heart = HeartbeatManager.Instance;
            string sanity = SanityManager.Instance != null ? $"{SanityManager.Instance.Current:0} → {SanityManager.Instance.Target:0}" : "—";
            string mode = heart == null ? "" : heart.Scripted ? " (white room)" : heart.Screamer ? " (screamer)" : "";
            string bpm = heart != null ? $"{heart.Bpm:0}{mode}" : "—";
            string light = FlashlightController.Instance != null
                ? $"{(FlashlightController.Instance.Available ? "available" : "unavailable")}, {(FlashlightController.Instance.IsOn ? "on" : "off")}"
                : "—";
            GUI.Box(new Rect(10, Screen.height - 190, 560, 180),
                $"P4 debug — Sanity: {sanity} | BPM: {bpm}\nFlashlight: {light}\n" +
                "1–5: sanity 90/70/50/25/0   0: sanity 100 (instant)\n" +
                "H: spike   S: screamer (160–180, then falls)   B: white room → bed (fade to 0) → off\n" +
                "L: flashlight available   F: flashlight   A/D: move\n" +
                "Shift+1–5: hallucinations H1–H5   U: unlock player",
                style);
        }
    }
}
