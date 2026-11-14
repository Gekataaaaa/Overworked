using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Открывает мини-игру поверх сцены и блокирует Дэнни на это время.
    /// ЗАГЛУШКА — заполняет P2.
    /// </summary>
    public class MiniGameHost : SingletonBehaviour<MiniGameHost>
    {
        /// <summary>Запускает мини-игру, по окончании возвращает управление и вызывает onFinish.</summary>
        public void Open(IMiniGame game, Action<bool> onFinish)
        {
            Debug.Log($"[MiniGames] Open {game}");
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(true);
            game.Begin(win =>
            {
                if (PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(false);
                onFinish?.Invoke(win);
            });
        }
    }
}
