using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Предмет без инвентаря: по E взлетает над головой Дэнни и втягивается в него (PickupFlyEffect), ставит флаг (setsFlag, например "MaxPass"),
    /// показывает тост «Received: …» и закрывает задание (completesObjective).
    /// </summary>
    public class PickupInteractable : InteractableBase
    {
        [Tooltip("Тост, например «Received: Max's pass». Пусто — без тоста")]
        [SerializeField] string receivedToast;

        [Tooltip("Реплика Дэнни после подбора («Got it.»)")]
        [SerializeField] DialogueData afterDialogue;

        [Tooltip("Что спрятать после подбора. Пусто — весь объект")]
        [SerializeField] GameObject hideOnPickup;

        /// <inheritdoc/>
        public override bool CanInteract => base.CanInteract && !used;

        protected override void OnInteract()
        {
            Toast(receivedToast);
            // Finish до выключения объекта: onDone должен дойти до сценария, пока объект ещё жив.
            Finish();
            PlayDialogue(afterDialogue);
            // Не исчезает сразу: взлетает над головой Дэнни и втягивается в него (PickupFlyEffect).
            PickupFlyEffect.Play(hideOnPickup != null ? hideOnPickup : gameObject);
        }
    }
}
