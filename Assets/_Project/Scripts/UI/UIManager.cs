using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Общие окна интерфейса: тосты, текст на весь экран, видимость частей HUD.
    /// Живёт в префабе _Systems, ссылки на элементы HUD проставляет Overworked → P3 → Build Phone UI.
    /// Готово: тосты, видимость телефона. ЗАГЛУШКА: текст на весь экран и остальные части HUD — P3, этап 1.
    /// </summary>
    public class UIManager : SingletonBehaviour<UIManager>
    {
        [Header("Тост")]
        [Tooltip("Плашка тоста (CanvasGroup — для плавного появления)")]
        [SerializeField] CanvasGroup toastGroup;

        [Tooltip("Текст тоста")]
        [SerializeField] TextMeshProUGUI toastText;

        [Tooltip("Сколько секунд тост висит на экране")]
        [SerializeField] float toastSeconds = 2.5f;

        [Tooltip("Длительность появления/исчезновения тоста, секунды")]
        [SerializeField] float toastFade = 0.25f;

        [Header("HUD")]
        [Tooltip("Интерфейс телефона — для SetHudVisible(Phone)")]
        [SerializeField] PhoneUI phoneUI;

        readonly Queue<string> toasts = new Queue<string>();
        Coroutine toastRoutine;

        void Start()
        {
            if (toastGroup != null) toastGroup.alpha = 0f;
        }

        /// <summary>
        /// Короткое уведомление: «Received: Charles's note». Несколько подряд показываются по очереди.
        /// Текст проходит через TextFormatter ({name}).
        /// </summary>
        public void ShowToast(string text)
        {
            Debug.Log($"[UI] Toast: {text}");
            if (toastGroup == null || toastText == null || string.IsNullOrEmpty(text)) return;
            toasts.Enqueue(TextFormatter.Format(text));
            if (toastRoutine == null) toastRoutine = StartCoroutine(PlayToasts());
        }

        IEnumerator PlayToasts()
        {
            while (toasts.Count > 0)
            {
                toastText.text = toasts.Dequeue();
                yield return FadeToast(1f);
                // Realtime: тост дочитывается, даже если время остановлено паузой.
                yield return new WaitForSecondsRealtime(toastSeconds);
                yield return FadeToast(0f);
            }
            toastRoutine = null;
        }

        IEnumerator FadeToast(float to)
        {
            float from = toastGroup.alpha;
            for (float t = 0; t < toastFade; t += Time.unscaledDeltaTime)
            {
                toastGroup.alpha = Mathf.Lerp(from, to, t / toastFade);
                yield return null;
            }
            toastGroup.alpha = to;
        }

        /// <summary>Текст на весь экран (записка, системное сообщение). closed — после закрытия игроком.</summary>
        public void ShowFullscreenText(string text, Action closed)
        {
            Debug.Log($"[UI] Fullscreen: {text}");
            closed?.Invoke();
        }

        /// <summary>Показать или спрятать часть HUD. Phone — ещё и выключает сам телефон.</summary>
        public void SetHudVisible(HudPart part, bool visible)
        {
            Debug.Log($"[UI] {part} visible={visible}");
            if (part == HudPart.Phone)
            {
                if (phoneUI != null) phoneUI.SetButtonVisible(visible);
                if (PhoneManager.Instance != null) PhoneManager.Instance.SetAvailable(visible);
            }
        }
    }
}
