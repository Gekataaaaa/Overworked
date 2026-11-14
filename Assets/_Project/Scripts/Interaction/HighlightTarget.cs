using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Что подсвечивать вместо этого объекта: невидимая «заглушка» с IInteractable (например,
    /// объяснение у запертой лестницы Floor_02) показывает жёлтый контур и подсказку на настоящем
    /// объекте — его спрайтах и границах. Пусто — подсвечивается сам объект.
    /// </summary>
    public class HighlightTarget : MonoBehaviour
    {
        [Tooltip("Объект, чьи спрайты обводить и над чьим верхом ставить подсказку")]
        public GameObject target;
    }
}
