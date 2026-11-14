using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Зона, которая выдаёт задание, когда Дэнни в неё входит (или, если completes, закрывает его).
    /// Для простых случаев без сценария; цепочки «сообщение → задание» собирает ScriptedSequence (P3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ObjectiveTrigger : MonoBehaviour
    {
        [Tooltip("Какое задание выдать (или закрыть)")]
        [SerializeField] ObjectiveData objective;

        [Tooltip("Закрыть задание вместо выдачи")]
        [SerializeField] bool completes;

        bool fired;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other.GetComponent<PlayerController>() == null || ObjectiveManager.Instance == null) return;
            fired = true;
            if (completes) ObjectiveManager.Instance.Complete(objective);
            else ObjectiveManager.Instance.StartObjective(objective);
        }

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }
}
