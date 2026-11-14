using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace Overworked
{
    /// <summary>
    /// Дверь: вход в комнату или выход из неё (затемнение → игрок в target → смена камеры → проявление)
    /// или запертая дверь с текстом («This doesn't look like the kitchen.»).
    /// Запертую ключом дверь (Room_102) открывает флаг unlockFlag ("Key102").
    /// Комнаты лежат в той же сцене ниже коридора; у каждой своя CinemachineCamera, выключенная на старте.
    /// </summary>
    public class DoorInteractable : InteractableBase
    {
        [Header("Переход")]
        [Tooltip("Куда поставить Дэнни: EntryPoint комнаты или точка у двери в коридоре. Пусто — дверь только заперта")]
        [SerializeField] Transform target;

        [Tooltip("Камера, которую включить (CM Room_X при входе, CM Corridor при выходе)")]
        [SerializeField] GameObject cameraOn;

        [Tooltip("Камера, которую выключить")]
        [SerializeField] GameObject cameraOff;

        [Tooltip("Длительность затемнения и проявления, секунд")]
        [SerializeField] float fadeTime = 0.35f;

        [Header("Замок")]
        [Tooltip("Дверь заперта (декоративные двери коридора — всегда)")]
        [SerializeField] bool doorLocked;

        [Tooltip("Флаг, который отпирает дверь (\"Key102\"). Пусто — ключа нет")]
        [SerializeField] string unlockFlag;

        [Tooltip("Подсказка у запертой двери")]
        [SerializeField] string lockedPrompt = "Open";

        [Tooltip("Тост при попытке открыть запертую дверь (английский)")]
        [SerializeField] string lockedMessage = "It's locked.";

        [Tooltip("Описание/реплики при попытке открыть запертую (табличка Room_102). Если задано — вместо тоста")]
        [SerializeField] DialogueData lockedDialogue;

        [Tooltip("Попытка открыть запертую дверь: сценарий выдаёт задание (Floor_01: «Find the key in room 101»)")]
        public UnityEvent onLockedTried;

        /// <summary>Сейчас заперта.</summary>
        public bool IsLocked => target == null || (doorLocked && !(HasUnlockFlag));

        bool HasUnlockFlag => !string.IsNullOrEmpty(unlockFlag) && HasFlag(unlockFlag);

        /// <inheritdoc/>
        public override string Prompt => IsLocked ? lockedPrompt : prompt;

        /// <summary>Отпереть (false) или запереть (true) дверь из сценария.</summary>
        public void SetDoorLocked(bool value) => doorLocked = value;

        protected override void OnInteract()
        {
            if (IsLocked)
            {
                if (lockedDialogue != null) PlayDialogue(lockedDialogue);
                else Toast(lockedMessage);
                onLockedTried?.Invoke();
                return;
            }
            StartCoroutine(Transfer());
        }

        IEnumerator Transfer()
        {
            busy = true;
            LockPlayer(true);
            yield return Fade(true);

            PlayerController.Instance.Teleport(target);
            yield return SwitchCameras();

            yield return Fade(false);
            LockPlayer(false);
            Finish();
        }

        IEnumerator Fade(bool toBlack)
        {
            var fade = FadeController.Instance;
            if (fade == null) yield break;
            bool done = false;
            if (toBlack) fade.FadeOut(fadeTime, () => done = true);
            else fade.FadeIn(fadeTime, () => done = true);
            while (!done) yield return null;
        }

        // Под затемнением камера должна переключиться резко: без этого Cinemachine 2 секунды
        // плывёт от коридора к комнате, и проявление показывает этот полёт.
        IEnumerator SwitchCameras()
        {
            var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
            var savedBlend = brain != null ? brain.DefaultBlend : default;
            if (brain != null) brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

            if (cameraOff != null) cameraOff.SetActive(false);
            if (cameraOn != null)
            {
                cameraOn.SetActive(true);
                var vcam = cameraOn.GetComponent<CinemachineVirtualCameraBase>();
                // Камера не помнит старую позицию — встаёт сразу на игрока, без доводки.
                if (vcam != null) vcam.PreviousStateIsValid = false;
            }
            // Brain выбирает новую камеру в LateUpdate: ждём кадр, потом возвращаем обычный блендинг.
            yield return null;
            yield return null;
            if (brain != null) brain.DefaultBlend = savedBlend;
        }
    }
}
