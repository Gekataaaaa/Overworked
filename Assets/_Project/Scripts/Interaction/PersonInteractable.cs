using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Бормочущий человек на стуле в Room_101: по E сжимается в точку и исчезает,
    /// затем реплика Дэнни («He wasn't here…»).
    /// </summary>
    public class PersonInteractable : InteractableBase
    {
        [Tooltip("Что сжимать. Пусто — весь объект")]
        [SerializeField] Transform visual;

        [Tooltip("За сколько секунд фигура сжимается в точку")]
        [SerializeField] float shrinkTime = 0.6f;

        [Tooltip("Реплики Дэнни после исчезновения")]
        [SerializeField] DialogueData afterDialogue;

        /// <inheritdoc/>
        public override bool CanInteract => base.CanInteract && !used;

        protected override void OnInteract() => StartCoroutine(Shrink());

        IEnumerator Shrink()
        {
            busy = true;
            LockPlayer(true);
            var t = visual != null ? visual : transform;
            Vector3 start = t.localScale;
            for (float time = 0f; time < shrinkTime; time += Time.deltaTime)
            {
                // Быстрее к концу: фигура «схлопывается», а не плавно тает.
                float k = time / shrinkTime;
                t.localScale = start * (1f - k * k);
                yield return null;
            }
            t.localScale = Vector3.zero;
            LockPlayer(false);
            Finish();
            PlayDialogue(afterDialogue);
            gameObject.SetActive(false);
        }
    }
}
