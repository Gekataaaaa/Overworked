using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Открытие/закрытие телефона (Tab или кнопка). Пока открыт — Дэнни стоит.
    /// ЗАГЛУШКА — заполняет P3.
    /// </summary>
    public class PhoneManager : SingletonBehaviour<PhoneManager>
    {
        /// <summary>Игрок закрыл телефон.</summary>
        public event Action OnClosed;

        /// <summary>Открыт ли сейчас телефон.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Открыть телефон.</summary>
        public void Open() { IsOpen = true; Debug.Log("[Phone] Open"); }

        /// <summary>Закрыть телефон.</summary>
        public void Close()
        {
            IsOpen = false;
            Debug.Log("[Phone] Close");
            OnClosed?.Invoke();
        }
    }
}
