using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Доставка сообщений в телефон: вибрация, уведомление, история.
    /// ЗАГЛУШКА: onReadAndClosed вызывается сразу — заполняет P3.
    /// </summary>
    public class MessageManager : SingletonBehaviour<MessageManager>
    {
        /// <summary>Доставляет сообщение. onReadAndClosed — когда игрок прочитал его и закрыл телефон.</summary>
        public void Deliver(MessageData msg, Action onReadAndClosed = null)
        {
            Debug.Log($"[Phone] Deliver {msg?.name}");
            if (msg != null && msg.LinkedObjective != null && ObjectiveManager.Instance != null)
                ObjectiveManager.Instance.StartObjective(msg.LinkedObjective);
            onReadAndClosed?.Invoke();
        }
    }
}
