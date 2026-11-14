using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Зона, которая запускает галлюцинацию, когда в неё входит Дэнни (середина коридора Floor_02 — H2).
    /// Без галлюцинации — просто отметка «Дэнни здесь был» для другой зоны: финальный скример срабатывает
    /// в коридоре у двери 102 только после того, как Дэнни побывал в Room_102.
    /// Для галлюцинаций внутри сценария (после телефона, мини-игры) — HallucinationStep, а не эта зона.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HallucinationTrigger : MonoBehaviour
    {
        [Tooltip("Какую галлюцинацию запустить (пусто — зона-отметка для requireVisited другой зоны)")]
        [SerializeField] HallucinationEvent hallucination;

        [Tooltip("Срабатывать, только если Дэнни уже побывал в этой зоне (пусто — без условия)")]
        [SerializeField] HallucinationTrigger requireVisited;

        [Tooltip("Срабатывать, только если эта галлюцинация уже была (пусто — без условия)")]
        [SerializeField] HallucinationEvent requirePlayed;

        [Tooltip("Сработать один раз")]
        [SerializeField] bool once = true;

        /// <summary>Дэнни входил в зону.</summary>
        public bool Visited { get; private set; }

        bool fired;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            Visited = true;
            if (hallucination == null || (fired && once)) return;
            if (requireVisited != null && !requireVisited.Visited) return;
            if (requirePlayed != null && !requirePlayed.HasPlayed) return;
            if (hallucination.IsPlaying) return;
            fired = true;
            if (HallucinationManager.Instance != null) HallucinationManager.Instance.Play(hallucination);
            else hallucination.Play();
        }

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }
}
