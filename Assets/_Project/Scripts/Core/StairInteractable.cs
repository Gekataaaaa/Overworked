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
        [SerializeField] string prompt = "Go down";

        [Tooltip("Заперта на старте (откроет сценарий через SetLocked(false))")]
        [SerializeField] bool startLocked;

        [Tooltip("Лестница сама ловит E и показывает подсказку — только для тестовых сцен без InteractionSystem. " +
                 "В обычных сценах выключено: E обрабатывает InteractionSystem, иначе нажатие сработает дважды")]
        [SerializeField] bool standaloneE;

        [Tooltip("Подсказка «[E] …» над лестницей для автономного режима")]
        [SerializeField] GameObject promptLabel;

        [Header("Звук спуска")]
        [Tooltip("Шаги по лестнице по E (Audio/SFX/SFX_StairsDown). Пусто — без звука")]
        [SerializeField] AudioClip descendSound;

        [Tooltip("Громкость звука спуска 0..1")]
        [Range(0f, 1f)] [SerializeField] float descendVolume = 1f;

        [Tooltip("Сколько секунд звук играет в полную громкость (переход между этажами — около 2 с)")]
        [SerializeField] float descendHold = 2.5f;

        [Tooltip("За сколько секунд звук затухает после этого")]
        [SerializeField] float descendFade = 1.5f;

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
            // Клип длиннее перехода: играет поверх затемнения и затухает уже на новом этаже, а не обрывается.
            if (descendSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfxFading(descendSound, descendVolume, descendHold, descendFade);
            GameManager.Instance.LoadFloor(targetFloor);
        }

        void ShowPrompt(bool visible)
        {
            if (promptLabel != null && promptLabel.activeSelf != visible) promptLabel.SetActive(visible);
        }
    }
}
