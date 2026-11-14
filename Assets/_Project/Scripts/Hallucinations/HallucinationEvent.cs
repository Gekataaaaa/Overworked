using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Одна галлюцинация (H1–H4, финальная): звук, визуал, спайк пульса, новое значение рассудка.
    /// Лежит в сцене (папка Hallucinations), запускается через HallucinationManager.
    /// </summary>
    public class HallucinationEvent : MonoBehaviour
    {
        [Tooltip("Рассудок после галлюцинации (паспорт, раздел 11)")]
        public float SanityAfter = 90f;

        [Tooltip("Насколько подскакивает пульс")]
        public float HeartbeatSpike = 30f;

        [Tooltip("Длительность, секунд")]
        public float Duration = 3f;
    }
}
