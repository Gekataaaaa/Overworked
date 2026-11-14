using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Общие окна интерфейса: тосты, текст на весь экран, видимость частей HUD.
    /// ЗАГЛУШКА — заполняет P3.
    /// </summary>
    public class UIManager : SingletonBehaviour<UIManager>
    {
        /// <summary>Короткое уведомление: «Received: Charles's note».</summary>
        public void ShowToast(string text) => Debug.Log($"[UI] Toast: {text}");

        /// <summary>Текст на весь экран (записка, системное сообщение). closed — после закрытия игроком.</summary>
        public void ShowFullscreenText(string text, Action closed)
        {
            Debug.Log($"[UI] Fullscreen: {text}");
            closed?.Invoke();
        }

        /// <summary>Показать или спрятать часть HUD.</summary>
        public void SetHudVisible(HudPart part, bool visible) => Debug.Log($"[UI] {part} visible={visible}");
    }
}
