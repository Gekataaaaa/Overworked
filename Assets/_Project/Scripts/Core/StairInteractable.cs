using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Лестница на следующий этаж: по E грузит этаж targetFloor.
    /// Лежит в префабе Prefabs/Environment/Staircase, ставится в объект Staircase сцены этажа.
    /// Пока путь закрыт «замком» этажа, сценарий держит её запертой: SetLocked(true).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StairInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Куда ведёт: 2, 1; 0 — WhiteRoom")]
        [SerializeField] int targetFloor = 2;

        [Tooltip("Подсказка после [E]")]
        [SerializeField] string prompt = "Go downstairs";

        [Tooltip("Заперта на старте (откроет сценарий через SetLocked(false))")]
        [SerializeField] bool startLocked;

        [Tooltip("ВРЕМЕННО: лестница сама ловит E и показывает подсказку, пока нет InteractionSystem (P2). " +
                 "Выключить, когда InteractionSystem заработает, иначе E обработается дважды")]
        [SerializeField] bool standaloneE = true;

        [Tooltip("Подсказка «[E] …» над лестницей для автономного режима")]
        [SerializeField] GameObject promptLabel;

        bool locked;
        bool used;
        bool playerInside;

        /// <inheritdoc/>
        public string Prompt => prompt;

        /// <inheritdoc/>
        public bool CanInteract => !locked && !used;

        void Awake()
        {
            locked = startLocked;
            ShowPrompt(false);
        }

        /// <summary>Запереть (true) или открыть (false) лестницу.</summary>
        public void SetLocked(bool value) => locked = value;

        void Update()
        {
            if (!standaloneE || !playerInside) return;
            ShowPrompt(CanInteract && PlayerController.Instance.State == PlayerState.Normal);
            if (GameInput.Interact != null && GameInput.Interact.WasPressedThisFrame()
                && PlayerController.Instance.State == PlayerState.Normal)
                Interact();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) playerInside = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() == null) return;
            playerInside = false;
            ShowPrompt(false);
        }

        /// <inheritdoc/>
        [ContextMenu("Interact")]
        public void Interact()
        {
            if (!CanInteract) return;
            // Защита от двойного E: второй вызов уже ничего не сделает, пока идёт переход.
            used = true;
            ShowPrompt(false);
            Debug.Log($"[Core] Stairs → floor {targetFloor}");
            GameManager.Instance.LoadFloor(targetFloor);
        }

        void ShowPrompt(bool visible)
        {
            if (promptLabel != null && promptLabel.activeSelf != visible) promptLabel.SetActive(visible);
        }
    }
}
