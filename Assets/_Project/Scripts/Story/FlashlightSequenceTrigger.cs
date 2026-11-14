using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Запускает сценарий, когда игрок впервые включает фонарик (Floor_02, нарратив 2.0: включил фонарик →
    /// сообщения Макса и Босса → задание «Check the documents in room 201»). Срабатывает один раз.
    /// Кладётся на объект со ScriptedSequence в группе Story; фонарик — FlashlightController (P4).
    /// </summary>
    public class FlashlightSequenceTrigger : MonoBehaviour
    {
        [Tooltip("Какой сценарий запустить (пусто — ScriptedSequence на этом же объекте)")]
        [SerializeField] ScriptedSequence sequence;

        bool subscribed, fired;

        void Awake()
        {
            if (sequence == null) sequence = GetComponent<ScriptedSequence>();
        }

        void Update()
        {
            // FlashlightController живёт в сцене этажа и может появиться позже нас — подписываемся, когда он есть.
            if (subscribed || fired || FlashlightController.Instance == null) return;
            FlashlightController.Instance.OnToggled += OnToggled;
            subscribed = true;
            if (FlashlightController.Instance.IsOn) OnToggled(true);
        }

        void OnDestroy()
        {
            if (subscribed && FlashlightController.Instance != null) FlashlightController.Instance.OnToggled -= OnToggled;
        }

        void OnToggled(bool on)
        {
            if (!on || fired || sequence == null) return;
            fired = true;
            if (FlashlightController.Instance != null) FlashlightController.Instance.OnToggled -= OnToggled;
            subscribed = false;
            sequence.Play();
        }
    }
}
