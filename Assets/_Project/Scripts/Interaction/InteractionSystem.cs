using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Ищет ближайший IInteractable рядом с игроком, показывает подсказку, по E вызывает Interact().
    /// ЗАГЛУШКА — заполняет P2.
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        [Tooltip("Радиус поиска объектов вокруг игрока")]
        [SerializeField] float radius = 1.2f;
    }
}
