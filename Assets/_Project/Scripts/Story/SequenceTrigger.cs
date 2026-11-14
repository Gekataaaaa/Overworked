using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Зона, которая запускает сценарий, когда в неё входит Дэнни: выход в коридор (сообщение Макса),
    /// середина коридора (Галлюцинация 2), вход в комнату. Нужен Collider2D-триггер.
    /// Можно ограничить заданием: например, сообщение после кухни — только когда пропуск уже взят.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SequenceTrigger : MonoBehaviour
    {
        [Tooltip("Какой сценарий запустить")]
        [SerializeField] ScriptedSequence sequence;

        [Tooltip("Срабатывать только если это задание уже выполнено (пусто — без условия)")]
        [SerializeField] ObjectiveData requireCompleted;

        [Tooltip("Не срабатывать, если это задание уже выполнено (пусто — без условия)")]
        [SerializeField] ObjectiveData blockIfCompleted;

        [Tooltip("Сработать один раз")]
        [SerializeField] bool once = true;

        bool fired;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (fired && once) return;
            if (sequence == null || other.GetComponent<PlayerController>() == null) return;
            if (!ConditionsMet()) return;
            // Сценарий уже идёт (игрок вышел и снова вошёл) — повторно не запускаем.
            if (sequence.IsPlaying) return;
            fired = true;
            sequence.Play();
        }

        // Игрок мог войти в зону до того, как условие выполнилось (стоит в ней, пока подбирает пропуск).
        void OnTriggerStay2D(Collider2D other)
        {
            if (!fired && requireCompleted != null) OnTriggerEnter2D(other);
        }

        bool ConditionsMet()
        {
            var om = ObjectiveManager.Instance;
            if (requireCompleted != null && (om == null || !om.IsCompleted(requireCompleted))) return false;
            if (blockIfCompleted != null && om != null && om.IsCompleted(blockIfCompleted)) return false;
            return true;
        }

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            if (sequence == null) sequence = GetComponent<ScriptedSequence>();
        }
    }
}
