using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Вспышки на весь экран: резкий свет ламп (H1), мигание света (H3), лампы в такт пульсу (H4),
    /// «искажение» после удара. Заглушка: полупрозрачный прямоугольник поверх экрана.
    /// </summary>
    public class ScreenFlashVisual : HallucinationVisual
    {
        [Tooltip("Цвет вспышки (белый — свет, чёрный — провал света)")]
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.8f);
        [Tooltip("Сколько вспышек")]
        [SerializeField] int count = 3;
        [Tooltip("Вспышка длится, секунд")]
        [SerializeField] float onTime = 0.08f;
        [Tooltip("Пауза между вспышками, секунд (случайно ±50 %)")]
        [SerializeField] float offTime = 0.15f;

        [Header("В такт пульсу")]
        [Tooltip("Мигать на каждый удар сердца (Room_101), а не по счётчику")]
        [SerializeField] bool syncToHeartbeat;
        [Tooltip("Сколько секунд мигать в такт пульсу")]
        [SerializeField] float heartbeatDuration = 6f;

        [Tooltip("Удар на первой вспышке (иначе — после последней)")]
        [SerializeField] bool hitOnFirst = true;

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
            var image = OverlaySquare("Flash", color);
            Stretch(image.rectTransform);
            image.enabled = false;

            if (syncToHeartbeat && HeartbeatManager.Instance != null)
            {
                var heart = HeartbeatManager.Instance;
                float flashUntil = 0f;
                void OnBeat() => flashUntil = Time.time + onTime;
                heart.OnBeat += OnBeat;
                SeenIfNeeded(hallucination);
                if (hitOnFirst) HitIfNeeded(hallucination);
                for (float t = 0f; t < heartbeatDuration; t += Time.deltaTime)
                {
                    image.enabled = Time.time < flashUntil;
                    yield return null;
                }
                heart.OnBeat -= OnBeat;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    image.enabled = true;
                    if (i == 0) SeenIfNeeded(hallucination);
                    if (i == 0 && hitOnFirst) HitIfNeeded(hallucination);
                    yield return new WaitForSeconds(onTime);
                    image.enabled = false;
                    yield return new WaitForSeconds(offTime * Random.Range(0.5f, 1.5f));
                }
            }
            HitIfNeeded(hallucination);
            Destroy(image.gameObject);
        }
    }
}
