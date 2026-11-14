using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Рассудок 0..100. Меняется только сценарными событиями; шкала плавно догоняет цель.
    /// ЗАГЛУШКА: плавность есть, остальное заполняет P4.
    /// </summary>
    public class SanityManager : SingletonBehaviour<SanityManager>
    {
        [Tooltip("За сколько секунд шкала догоняет новое значение")]
        [SerializeField] float smoothTime = 1f;

        /// <summary>Рассудок изменился; аргумент — текущее значение 0..1.</summary>
        public event Action<float> OnSanityChanged;

        /// <summary>Текущее (анимированное) значение 0..100.</summary>
        public float Current { get; private set; } = 100f;

        /// <summary>Целевое значение 0..100.</summary>
        public float Target { get; private set; } = 100f;

        float velocity;

        /// <summary>Новое значение рассудка, шкала придёт к нему за ~1 сек.</summary>
        public void SetSanity(float value)
        {
            Target = Mathf.Clamp(value, 0f, 100f);
            Debug.Log($"[Sanity] → {Target}");
        }

        /// <summary>Без анимации — при загрузке этажа.</summary>
        public void SetSanityInstant(float value)
        {
            Target = Current = Mathf.Clamp(value, 0f, 100f);
            OnSanityChanged?.Invoke(Current / 100f);
        }

        void Update()
        {
            if (Mathf.Approximately(Current, Target)) return;
            Current = Mathf.SmoothDamp(Current, Target, ref velocity, smoothTime / 3f);
            if (Mathf.Abs(Current - Target) < 0.05f) Current = Target;
            OnSanityChanged?.Invoke(Current / 100f);
        }
    }
}
