using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Ящик стола / тумбы: по E открывается, при необходимости показывает документ листом A4
    /// (DocumentSheet), затем содержимое (описание и реплики Дэнни), пауза и вторая порция реплик,
    /// затем флаг setsFlag и тост.
    /// Room_201 — графики смен (пауза, «Wait. That's your name, isn't it?»), Room_101 — бейдж.
    /// </summary>
    public class DrawerInteractable : InteractableBase
    {
        [Tooltip("Документ из ящика — показывается листом A4 до реплик (строки ассета через перенос, {name} работает). Пусто — без листа")]
        [SerializeField] DialogueData documentSheet;

        [Tooltip("Описание содержимого и реплики Дэнни (ассет из Data/Dialogues)")]
        [SerializeField] DialogueData contentsDialogue;

        [Tooltip("Пауза перед второй порцией реплик, секунд (нарратив: «Danny pauses»)")]
        [SerializeField] float pauseBeforeAfter = 1.5f;

        [Tooltip("Реплики после паузы. Пусто — без паузы и второй порции")]
        [SerializeField] DialogueData afterPauseDialogue;

        [Tooltip("Тост после осмотра, например «Received: Key to room 102». Пусто — без тоста")]
        [SerializeField] string receivedToast;

        [Tooltip("Спрайт ящика: сменится на openSprite. Пусто — без смены")]
        [SerializeField] SpriteRenderer drawerSprite;

        [Tooltip("Картинка открытого ящика")]
        [SerializeField] Sprite openSprite;

        [Header("Звук")]
        [Tooltip("Звук открытия ящика (Audio/SFX/SFX_DrawerOpen). Пусто — без звука")]
        [SerializeField] AudioClip openSound;

        [Tooltip("Звук закрытия ящика (Audio/SFX/SFX_DrawerClose). Пусто — без звука")]
        [SerializeField] AudioClip closeSound;

        [Tooltip("Шорох бумаги, когда из ящика достают документ (documentSheet; Audio/SFX/SFX_Paper). Пусто — без звука")]
        [SerializeField] AudioClip paperSound;

        [Tooltip("Громкость звуков ящика 0..1")]
        [Range(0f, 1f)] [SerializeField] float soundVolume = 0.8f;

        [Tooltip("Закрыть ящик, когда содержимое осмотрено: звук закрытия и спрайт обратно в закрытый")]
        [SerializeField] bool closeWhenDone = true;

        Sprite closedSprite;

        /// <inheritdoc/>
        public override bool CanInteract => base.CanInteract && !used;

        protected override void OnInteract()
        {
            busy = true;
            PlaySound(openSound);
            if (drawerSprite != null && openSprite != null)
            {
                closedSprite = drawerSprite.sprite;
                drawerSprite.sprite = openSprite;
            }
            if (documentSheet != null)
            {
                PlaySound(paperSound);
                DocumentSheet.Show(SheetText(), PlayContents);
            }
            else PlayContents();
        }

        void PlayContents()
        {
            PlayDialogue(contentsDialogue, () =>
            {
                if (afterPauseDialogue != null) StartCoroutine(AfterPause());
                else Done();
            });
        }

        string SheetText()
        {
            if (documentSheet.Lines == null) return "";
            var lines = new string[documentSheet.Lines.Length];
            for (int i = 0; i < lines.Length; i++) lines[i] = TextFormatter.Format(documentSheet.Lines[i].Text);
            return string.Join("\n", lines);
        }

        IEnumerator AfterPause()
        {
            yield return new WaitForSeconds(pauseBeforeAfter);
            PlayDialogue(afterPauseDialogue, Done);
        }

        void Done()
        {
            // Дэнни посмотрел, что внутри, — ящик задвигается обратно.
            if (closeWhenDone)
            {
                PlaySound(closeSound);
                if (drawerSprite != null && closedSprite != null) drawerSprite.sprite = closedSprite;
            }
            Toast(receivedToast);
            Finish();
        }

        void PlaySound(AudioClip clip)
        {
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfx(clip, soundVolume);
        }
    }
}
