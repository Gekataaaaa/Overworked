using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Точка на пути Дэнни в WhiteRoom: при первом входе игрока срабатывает один раз —
    /// реплики, проявление предмета (кровать «появляется»), короткая остановка Дэнни
    /// или «лечь на кровать». Нужен Collider2D с галочкой Is Trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WhiteRoomTrigger : MonoBehaviour
    {
        [Tooltip("Сценарий комнаты. Пусто — ищется в сцене")]
        [SerializeField] WhiteRoomSequence sequence;
        [Tooltip("Реплики Дэнни в этой точке (можно пусто)")]
        [SerializeField] DialogueData lines;
        [Tooltip("Что плавно проявляется в этой точке (кровать)")]
        [SerializeField] SpriteRenderer[] reveal;
        [Tooltip("За сколько секунд проявляется reveal")]
        [SerializeField] float revealTime = 2f;
        [Tooltip("Дэнни останавливается на столько секунд («замечает кровать»); 0 — не останавливается")]
        [SerializeField] float holdPlayer;
        [Tooltip("Здесь Дэнни ложится на кровать — дальше финал")]
        [SerializeField] bool lieDown;

        bool fired;

        void Awake()
        {
            if (sequence == null) sequence = FindAnyObjectByType<WhiteRoomSequence>();
            // Предмет «появляется» — до этой точки его нет.
            foreach (var sr in reveal) SetAlpha(sr, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other.GetComponentInParent<PlayerController>() == null) return;
            if (sequence != null && sequence.InBed) return;
            fired = true;

            if (lieDown)
            {
                if (sequence != null) sequence.LieDown();
                return;
            }
            if (sequence != null) sequence.Say(lines);
            if (reveal.Length > 0) StartCoroutine(Reveal());
            if (holdPlayer > 0f) StartCoroutine(Hold());
        }

        IEnumerator Reveal()
        {
            for (float t = 0f; t < revealTime; t += Time.deltaTime)
            {
                foreach (var sr in reveal) SetAlpha(sr, t / revealTime);
                yield return null;
            }
            foreach (var sr in reveal) SetAlpha(sr, 1f);
        }

        IEnumerator Hold()
        {
            var player = PlayerController.Instance;
            if (player == null) yield break;
            player.SetInputLocked(true);
            yield return new WaitForSeconds(holdPlayer);
            // Пока стояли, мог сработать финал — тогда управление уже не возвращаем.
            if (sequence == null || !sequence.InBed) player.SetInputLocked(false);
        }

        static void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
