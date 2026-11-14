using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Ящик стола / тумбы: по E открывается, показывает содержимое (описание и реплики Дэнни),
    /// затем выдаёт предмет: флаг setsFlag и тост. Room_201 — графики смен, Room_101 — ключ и бейдж.
    /// </summary>
    public class DrawerInteractable : InteractableBase
    {
        [Tooltip("Описание содержимого и реплики Дэнни (ассет из Data/Dialogues)")]
        [SerializeField] DialogueData contentsDialogue;

        [Tooltip("Тост после осмотра, например «Received: Key to room 102». Пусто — без тоста")]
        [SerializeField] string receivedToast;

        [Tooltip("Спрайт ящика: сменится на openSprite. Пусто — без смены")]
        [SerializeField] SpriteRenderer drawerSprite;

        [Tooltip("Картинка открытого ящика")]
        [SerializeField] Sprite openSprite;

        /// <inheritdoc/>
        public override bool CanInteract => base.CanInteract && !used;

        protected override void OnInteract()
        {
            busy = true;
            if (drawerSprite != null && openSprite != null) drawerSprite.sprite = openSprite;
            PlayDialogue(contentsDialogue, () =>
            {
                Toast(receivedToast);
                Finish();
            });
        }
    }
}
