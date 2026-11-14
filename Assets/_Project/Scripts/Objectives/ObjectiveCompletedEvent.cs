using UnityEngine;
using UnityEngine.Events;

namespace Overworked
{
    /// <summary>
    /// Срабатывает один раз, когда выполнено задание с указанным Id (неважно, кто его закрыл:
    /// объект P2, шаг сценария P3). Если задание уже выполнено к старту сцены — сработает сразу.
    /// Пример — Floor_02: F2_CheckDocs201 выполнено → лестница отпирается (StairInteractable.SetLocked(false)).
    /// </summary>
    public class ObjectiveCompletedEvent : MonoBehaviour
    {
        [Tooltip("Id задания (ObjectiveData.Id), например F2_CheckDocs201")]
        [SerializeField] string objectiveId;

        [Tooltip("Что сделать, когда задание выполнено")]
        public UnityEvent onCompleted;

        bool fired;

        void Start()
        {
            var om = ObjectiveManager.Instance;
            if (om == null) return;
            om.OnCompleted += Check;
            if (om.IsCompleted(objectiveId)) Fire();
        }

        void OnDestroy()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.OnCompleted -= Check;
        }

        void Check(ObjectiveData o)
        {
            if (o != null && o.Id == objectiveId) Fire();
        }

        void Fire()
        {
            if (fired) return;
            fired = true;
            Debug.Log($"[Objectives] {objectiveId} выполнено → {name}");
            onCompleted?.Invoke();
        }
    }
}
