using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Вместе с одним заданием выдаёт и другое: кто бы ни выдал whenStarted (зона, дверь, сообщение),
    /// следом выдаётся alsoStart. Если whenStarted закрыли раньше, чем выдали (Floor_01: зашёл в 101 и взял ключ,
    /// не подходя к двери 102), alsoStart всё равно выдаётся — иначе обязательное задание потерялось бы.
    /// Floor_01: «Find the key in room 101» → ещё «Talk to the man in room 101».
    /// Кладётся в сцену этажа (объект в группе Story).
    /// </summary>
    public class ObjectiveLink : MonoBehaviour
    {
        [Tooltip("Когда выдано это задание…")]
        [SerializeField] ObjectiveData whenStarted;

        [Tooltip("…выдать ещё и это")]
        [SerializeField] ObjectiveData alsoStart;

        bool subscribed;

        void OnEnable() => TrySubscribe();

        // Запасной вариант: сцена может включиться раньше, чем _Systems создаст ObjectiveManager.
        void Start() => TrySubscribe();

        void OnDisable()
        {
            if (subscribed && ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnStarted -= OnStarted;
                ObjectiveManager.Instance.OnCompleted -= OnStarted;
            }
            subscribed = false;
        }

        void TrySubscribe()
        {
            if (subscribed || ObjectiveManager.Instance == null) return;
            ObjectiveManager.Instance.OnStarted += OnStarted;
            ObjectiveManager.Instance.OnCompleted += OnStarted;
            subscribed = true;
            // Задание могли выдать (или выполнить) до нас — догоняем.
            if (ObjectiveManager.Instance.IsActive(whenStarted) || ObjectiveManager.Instance.IsCompleted(whenStarted))
                OnStarted(whenStarted);
        }

        void OnStarted(ObjectiveData o)
        {
            if (ObjectiveManager.Same(o, whenStarted) && alsoStart != null)
                ObjectiveManager.Instance.StartObjective(alsoStart);
        }
    }
}
