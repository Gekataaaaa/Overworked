using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Запрет возврата (Floor_02, нарратив 2.2): зона позади Дэнни. Когда он пытается пойти назад,
    /// Дэнни останавливается, появляется «Do you still want Danny to keep working here?» и его реплика
    /// (ассет F2_NoReturn), затем его отводит обратно из зоны. Срабатывает при каждой попытке.
    /// Нужен Collider2D-триггер; ставится между игроком и дорогой назад.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NoReturnTrigger : MonoBehaviour
    {
        [Tooltip("Системная строка + реплика Дэнни (Data/Dialogues/F2_NoReturn)")]
        [SerializeField] DialogueData dialogue;

        [Tooltip("Работать только после этого задания (на Floor_02 — F2_CheckDocs201). Пусто — всегда")]
        [SerializeField] ObjectiveData activeAfter;

        [Tooltip("Куда отвести Дэнни после реплик. Пусто — сдвинуть на pushDistance в сторону pushDirection")]
        [SerializeField] Transform returnPoint;

        [Tooltip("Куда отводить Дэнни, если returnPoint пуст: +1 — вправо (вперёд по этажу), -1 — влево")]
        [SerializeField] int pushDirection = 1;

        [Tooltip("На сколько юнитов отвести Дэнни, если returnPoint пуст")]
        [SerializeField] float pushDistance = 1.5f;

        [Tooltip("Пауза перед репликами — «Дэнни останавливается на мгновение», секунды")]
        [SerializeField] float pause = 0.4f;

        [Tooltip("Полные реплики только в первый раз; дальше — только системная строка")]
        [SerializeField] bool shortAfterFirst = true;

        bool busy;
        bool shownOnce;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (busy) return;
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;
            if (activeAfter != null && (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.IsCompleted(activeAfter)))
                return;
            StartCoroutine(Run(player));
        }

        IEnumerator Run(PlayerController player)
        {
            busy = true;
            Debug.Log("[Story] No return");
            // Если Дэнни уже держит сценарий, управление не трогаем — вернёт сценарий.
            bool locked = player.State == PlayerState.Normal;
            if (locked) player.SetInputLocked(true);

            if (pause > 0f) yield return new WaitForSeconds(pause);

            var dm = DialogueManager.Instance;
            if (dialogue != null && dm != null)
            {
                var data = shortAfterFirst && shownOnce ? FirstLineOnly(dialogue) : dialogue;
                yield return ScriptedSequence.WaitCallback(cb => dm.Play(data, cb));
            }
            shownOnce = true;

            if (player != null)
            {
                if (returnPoint != null) player.Teleport(returnPoint);
                else player.Teleport(player.transform.position + Vector3.right * Mathf.Sign(pushDirection) * pushDistance);
            }

            if (locked && player != null) player.SetInputLocked(false);
            busy = false;
        }

        // Повторные попытки: только «Do you still want Danny…», без повторной реплики Дэнни.
        DialogueData shortCopy;
        DialogueData FirstLineOnly(DialogueData full)
        {
            if (full.Lines == null || full.Lines.Length <= 1) return full;
            if (shortCopy == null)
            {
                shortCopy = ScriptableObject.CreateInstance<DialogueData>();
                shortCopy.name = full.name + "_Short";
                shortCopy.Lines = new[] { full.Lines[0] };
            }
            return shortCopy;
        }

        void OnDestroy()
        {
            if (shortCopy != null) Destroy(shortCopy);
        }

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }
    }
}
