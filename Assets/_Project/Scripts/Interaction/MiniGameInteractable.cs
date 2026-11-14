using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Объект, который по E открывает мини-игру через MiniGameHost. Победа — Finish()
    /// (задание, флаг, onDone), выход без победы — можно подойти снова.
    /// Основа для компьютера (Room_201) и аркадного автомата (Room_102).
    /// </summary>
    public abstract class MiniGameInteractable : InteractableBase
    {
        [Tooltip("Компонент мини-игры (реализует IMiniGame): на этом же объекте или отдельно")]
        [SerializeField] MonoBehaviour miniGame;

        [Tooltip("Реплики после победы")]
        [SerializeField] DialogueData winDialogue;

        /// <inheritdoc/>
        public override bool CanInteract => base.CanInteract && !used && Game != null;

        IMiniGame Game => miniGame as IMiniGame;

        protected override void OnInteract()
        {
            if (MiniGameHost.Instance == null)
            {
                Debug.LogError("[Interaction] нет MiniGameHost в _Systems");
                return;
            }
            busy = true;
            MiniGameHost.Instance.Open(Game, win =>
            {
                busy = false;
                if (!win) return;
                Finish();
                PlayDialogue(winDialogue);
            });
        }

        void OnValidate()
        {
            if (miniGame != null && miniGame is not IMiniGame)
            {
                Debug.LogWarning($"[Interaction] {name}: {miniGame.GetType().Name} не реализует IMiniGame");
                miniGame = null;
            }
        }
    }
}
