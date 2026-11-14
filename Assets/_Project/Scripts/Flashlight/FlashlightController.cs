using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Фонарик Дэнни (F), доступен с Floor_02. ЗАГЛУШКА — заполняет P4.
    /// </summary>
    public class FlashlightController : SingletonBehaviour<FlashlightController>
    {
        /// <summary>Фонарик включили/выключили.</summary>
        public event Action<bool> OnToggled;

        /// <summary>Включён ли фонарик.</summary>
        public bool IsOn { get; private set; }

        /// <summary>Доступен ли фонарик (на Floor_03 — нет).</summary>
        public bool Available { get; private set; }

        /// <summary>Сделать фонарик доступным/недоступным; недоступный выключается.</summary>
        public void SetAvailable(bool available)
        {
            Available = available;
            if (!available && IsOn) Toggle();
        }

        /// <summary>Переключить фонарик.</summary>
        public void Toggle()
        {
            if (!Available && !IsOn) return;
            IsOn = !IsOn;
            Debug.Log($"[Flashlight] {(IsOn ? "on" : "off")}");
            OnToggled?.Invoke(IsOn);
        }
    }
}
