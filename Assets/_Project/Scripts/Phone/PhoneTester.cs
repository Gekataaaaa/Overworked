using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworked
{
    /// <summary>
    /// Отладка телефона в Sandbox_P3: цифры 1–9 доставляют сообщения из списка,
    /// 0 — первые два подряд (цепочка «Макс, следом Босс»). В консоль пишется, когда
    /// сработал колбэк «прочитал и закрыл». В сцены этажей не класть.
    /// </summary>
    public class PhoneTester : MonoBehaviour
    {
        [Tooltip("Тестовые сообщения: клавиша 1 — первое, 2 — второе…")]
        [SerializeField] MessageData[] messages;

        static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || messages == null || MessageManager.Instance == null) return;

            for (int i = 0; i < DigitKeys.Length && i < messages.Length; i++)
                if (kb[DigitKeys[i]].wasPressedThisFrame) Send(messages[i]);

            if (kb[Key.Digit0].wasPressedThisFrame && messages.Length >= 2)
            {
                Send(messages[0]);
                Send(messages[1]);
            }
        }

        void Send(MessageData msg)
        {
            if (msg == null) return;
            MessageManager.Instance.Deliver(msg, () => Debug.Log($"[Phone] прочитано и закрыто: {msg.name}"));
        }
    }
}
