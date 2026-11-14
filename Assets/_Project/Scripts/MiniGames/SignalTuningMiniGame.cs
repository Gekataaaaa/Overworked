using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Мини-игра 3 «Настройка сигнала монитора» (компьютер в Room_201, нарратив сцена 2.2):
    /// экран в помехах и ряби, под ними — системное сообщение (по нарративу «REPORT NOT CLOSED.
    /// SHIFT CONTINUES.»), внизу два ползунка — «Frequency» и «Sharpness». Суть — «просветить» текст:
    /// каждый ползунок, подведённый к своему скрытому значению, убирает свою часть помех, и текст
    /// постепенно проступает. Проступил почти полностью — мини-игра выиграна. Ползунок у цели подсвечивается.
    /// Управление (паспорт, раздел 9): W/S — выбрать ползунок, A/D — двигать; мышью — тянуть ползунок. Esc — отойти.
    /// Оверлей строится из кода. Кладётся на объект компьютера, ссылку отдают в ComputerInteractable.miniGame.
    /// </summary>
    public class SignalTuningMiniGame : MonoBehaviour, IMiniGame
    {
        [Tooltip("Сообщение на экране (ассет F2_Monitor201): строки складываются через перенос")]
        [SerializeField] DialogueData screenText;

        [Tooltip("С какого расстояния до цели (доля шкалы) ползунок начинает «просвечивать» текст. Больше — легче")]
        [Range(0.2f, 0.8f)]
        [SerializeField] float revealWindow = 0.45f;

        [Tooltip("Насколько проявиться тексту для победы (0..1). Меньше — легче")]
        [Range(0.5f, 1f)]
        [SerializeField] float winClarity = 0.75f;

        [Tooltip("Скорость ползунка с клавиатуры: долей шкалы в секунду")]
        [SerializeField] float keySpeed = 0.6f;

        [Tooltip("Сколько секунд текст должен держаться проявленным, чтобы засчитать")]
        [SerializeField] float holdClearTime = 0.25f;

        [Tooltip("Сколько секунд видно проявившееся сообщение перед закрытием")]
        [SerializeField] float messageTime = 2.5f;

        [Header("Тексты интерфейса (английский)")]
        [SerializeField] string frequencyLabel = "Frequency";
        [SerializeField] string sharpnessLabel = "Sharpness";
        [SerializeField] string hint = "[W/S] select   [A/D] adjust   [Esc] step back";

        const int NoiseW = 96, NoiseH = 54;
        const float BarWidth = 520f;

        Action<bool> onFinish;
        Canvas canvas;
        RawImage noise;
        Texture2D noiseTex;
        Color32[] pixels;
        TMP_Text message;
        RectTransform screen;
        readonly RectTransform[] handles = new RectTransform[2];
        readonly Image[] handleImages = new Image[2];
        readonly RectTransform[] bars = new RectTransform[2];
        readonly TMP_Text[] labels = new TMP_Text[2];
        readonly float[] value = new float[2];
        readonly float[] target = new float[2];
        int selected;
        int dragging = -1;
        float clearFor;
        bool done;

        /// <inheritdoc/>
        public void Begin(Action<bool> finish)
        {
            onFinish = finish;
            if (canvas == null) Build();
            // Цели — в середине шкалы, старт — немного мимо: текст сразу чуть виден, но не читается.
            for (int i = 0; i < 2; i++)
            {
                target[i] = UnityEngine.Random.Range(0.3f, 0.7f);
                float offset = UnityEngine.Random.Range(0.3f, 0.4f);
                value[i] = Mathf.Clamp01(target[i] + (target[i] < 0.5f ? offset : -offset));
            }
            selected = 0;
            dragging = -1;
            clearFor = 0f;
            done = false;
            message.text = ScreenText();
            canvas.gameObject.SetActive(true);
            Refresh();
        }

        void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf || done) return;

            if (GameInput.Pause != null && GameInput.Pause.WasPressedThisFrame())
            {
                StartCoroutine(QuitNextFrame());
                return;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) selected = 0;
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) selected = 1;
                float dir = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir += 1f;
                value[selected] = Mathf.Clamp01(value[selected] + dir * keySpeed * Time.deltaTime);
            }
            HandleMouse();
            Refresh();

            float clarity = Clarity();
            clearFor = clarity >= winClarity ? clearFor + Time.deltaTime : 0f;
            if (clearFor >= holdClearTime) StartCoroutine(Win());
        }

        // Мышь: нажал на шкалу — ползунок выбран и едет за курсором, пока кнопка зажата.
        void HandleMouse()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;
            Vector2 pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                for (int i = 0; i < 2; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(bars[i], pos, null)) dragging = selected = i;
            }
            if (!mouse.leftButton.isPressed) dragging = -1;
            if (dragging < 0) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bars[dragging], pos, null, out var local);
            value[dragging] = Mathf.Clamp01(local.x / BarWidth + 0.5f);
        }

        // Насколько ползунок i у цели: 0 — дальше revealWindow, 1 — точно на ней.
        float Closeness(int i) => Mathf.Clamp01(1f - Mathf.Abs(value[i] - target[i]) / revealWindow);

        // Насколько проявлен текст: среднее двух ползунков — каждый «просвечивает» свою половину.
        float Clarity() => (Closeness(0) + Closeness(1)) / 2f;

        void Refresh()
        {
            for (int i = 0; i < 2; i++)
            {
                handles[i].anchoredPosition = new Vector2((value[i] - 0.5f) * BarWidth, 0f);
                // Подсказка: выбранный ползунок — ярче; ползунок у цели — зелёный.
                labels[i].color = i == selected ? Color.white : new Color(0.6f, 0.62f, 0.58f);
                handleImages[i].color = Color.Lerp(new Color(0.95f, 0.92f, 0.8f), new Color(0.35f, 1f, 0.5f),
                    Mathf.InverseLerp(0.6f, 0.9f, Closeness(i)));
            }

            // «Частота» — рябь (текст дрожит), «Резкость» — сила помех. Каждый ползунок убирает своё.
            float freqErr = 1f - Closeness(0);
            float sharpErr = 1f - Closeness(1);
            float clarity = Clarity();
            DrawNoise(sharpErr * 0.85f);
            message.rectTransform.anchoredPosition = new Vector2(
                Mathf.Sin(Time.time * 37f) * 30f * freqErr, Mathf.Sin(Time.time * 23f) * 5f * freqErr);
            // Текст всегда чуть виден сквозь помехи — понятно, что его надо «просветить».
            message.alpha = Mathf.Lerp(0.15f, 1f, clarity);
        }

        void DrawNoise(float amount)
        {
            byte level = (byte)(amount * 255f);
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)(UnityEngine.Random.value * level);
                pixels[i] = new Color32(v, v, v, level);
            }
            noiseTex.SetPixels32(pixels);
            noiseTex.Apply(false);
        }

        IEnumerator Win()
        {
            done = true;
            for (int i = 0; i < 2; i++) value[i] = target[i];
            Refresh();
            DrawNoise(0f);
            message.alpha = 1f;
            yield return new WaitForSeconds(messageTime);
            End(true);
        }

        // Кадром позже: в этом кадре Esc ещё «нажата», и меню паузы открылось бы сразу после выхода.
        IEnumerator QuitNextFrame()
        {
            done = true;
            yield return null;
            End(false);
        }

        void End(bool win)
        {
            StopAllCoroutines();
            canvas.gameObject.SetActive(false);
            var cb = onFinish;
            onFinish = null;
            cb?.Invoke(win);
        }

        string ScreenText()
        {
            if (screenText == null || screenText.Lines == null || screenText.Lines.Length == 0) return "";
            var parts = new string[screenText.Lines.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = TextFormatter.Format(screenText.Lines[i].Text);
            return string.Join("\n", parts);
        }

        void Build()
        {
            canvas = OverlayUi.CreateCanvas(transform, "SignalTuningOverlay", 200);
            OverlayUi.Fill(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
            var panel = OverlayUi.Box(canvas.transform, "Monitor", new Color(0.1f, 0.11f, 0.1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 760f)).transform;

            // Экран монитора: под помехами — сообщение, поверх — текстура шума.
            screen = OverlayUi.Box(panel, "Screen", new Color(0.02f, 0.05f, 0.03f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(720f, 405f)).rectTransform;
            message = OverlayUi.Label(screen, "Message", "", 46, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 200f));
            message.color = new Color(0.55f, 1f, 0.65f);
            var noiseGo = new GameObject("Noise", typeof(RectTransform));
            noiseGo.transform.SetParent(screen, false);
            var nrt = (RectTransform)noiseGo.transform;
            nrt.anchorMin = Vector2.zero;
            nrt.anchorMax = Vector2.one;
            nrt.sizeDelta = Vector2.zero;
            noise = noiseGo.AddComponent<RawImage>();
            noise.raycastTarget = false;
            noiseTex = new Texture2D(NoiseW, NoiseH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            pixels = new Color32[NoiseW * NoiseH];
            noise.texture = noiseTex;

            // Два ползунка под экраном.
            for (int i = 0; i < 2; i++)
            {
                float y = 210f - i * 90f;
                labels[i] = OverlayUi.Label(panel, "Label" + i, i == 0 ? frequencyLabel : sharpnessLabel, 26,
                    new Vector2(0.5f, 0f), new Vector2(0f, y + 30f), new Vector2(520f, 34f));
                bars[i] = OverlayUi.Box(panel, "Bar" + i, new Color(0.05f, 0.05f, 0.05f), new Vector2(0.5f, 0f),
                    new Vector2(0f, y), new Vector2(BarWidth, 18f)).rectTransform;
                handleImages[i] = OverlayUi.Box(bars[i], "Handle", new Color(0.95f, 0.92f, 0.8f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(16f, 36f));
                handles[i] = handleImages[i].rectTransform;
            }
            OverlayUi.Label(panel, "Hint", hint, 22, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(760f, 34f))
                .color = new Color(0.7f, 0.72f, 0.68f);

            canvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (noiseTex != null) Destroy(noiseTex);
        }
    }
}
