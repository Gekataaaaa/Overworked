using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Выдаёт или закрывает задание по вызову из UnityEvent (onDone, onLockedTried, onDenied…):
    /// так задание цепляется к событию объекта в инспекторе без своего скрипта.
    /// Пример: запертая дверь 102 → onLockedTried → Give() → «Find the key in room 101».
    /// </summary>
    public class ObjectiveGiver : MonoBehaviour
    {
        [Tooltip("Задание для Give() / Complete()")]
        [SerializeField] ObjectiveData objective;

        /// <summary>Выдать задание.</summary>
        public void Give()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.StartObjective(objective);
        }

        /// <summary>Закрыть задание.</summary>
        public void Complete()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Complete(objective);
        }
    }
}
