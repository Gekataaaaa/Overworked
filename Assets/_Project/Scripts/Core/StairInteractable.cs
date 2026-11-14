using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Лестница на следующий этаж: по E (через InteractionSystem) грузит этаж targetFloor.
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

        bool locked;
        bool used;

        /// <inheritdoc/>
        public string Prompt => prompt;

        /// <inheritdoc/>
        public bool CanInteract => !locked && !used;

        void Awake() => locked = startLocked;

        /// <summary>Запереть (true) или открыть (false) лестницу.</summary>
        public void SetLocked(bool value) => locked = value;

        /// <inheritdoc/>
        [ContextMenu("Interact")]
        public void Interact()
        {
            if (!CanInteract) return;
            // Защита от двойного E: второй вызов уже ничего не сделает, пока идёт переход.
            used = true;
            Debug.Log($"[Core] Stairs → floor {targetFloor}");
            GameManager.Instance.LoadFloor(targetFloor);
        }
    }
}
