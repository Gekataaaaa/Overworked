using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Реплики посреди галлюцинации (финальный скример: Дэнни замирает — «No…» — и только потом бросок).
    /// Текст — DialogueData в Data/Dialogues, показывает DialogueManager (P3).
    /// </summary>
    public class DialogueVisual : HallucinationVisual
    {
        [Tooltip("Реплики (ассет Data/Dialogues)")]
        [SerializeField] DialogueData lines;

        [Tooltip("Ждать, пока реплики покажутся до конца (иначе — сразу дальше)")]
        [SerializeField] bool wait = true;

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
            var dm = DialogueManager.Instance;
            if (lines == null || dm == null) yield break;
            bool done = false;
            // Реплики галлюцинации сменяются сами (не ждут нажатия): это катсцена, Дэнни заблокирован.
            dm.Play(lines, () => done = true, false);
            SeenIfNeeded(hallucination);
            HitIfNeeded(hallucination);
            if (!wait) yield break;
            // Страховка: если DialogueManager не вызовет done (его остановили), не висим вечно.
            for (float t = 0f; !done && t < 15f; t += Time.deltaTime) yield return null;
        }
    }
}
