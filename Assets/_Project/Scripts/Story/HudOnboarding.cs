using System;
using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Онбординг HUD в начале Floor_03 (нарратив, сцена 1.0): при старте этажа HUD спрятан, после реплик
    /// Дэнни рассудок, пульс и телефон появляются одновременно, затем — подсказка управления.
    /// Когда игрок закрыл подсказку, онбординг заканчивается. Кладётся в группу Story сцены Floor_03,
    /// запускается шагом сценария HudOnboarding (или <see cref="Play"/>).
    /// </summary>
    public class HudOnboarding : MonoBehaviour
    {
        [Tooltip("Спрятать HUD при старте сцены (на Floor_03 — да; в песочнице можно выключить)")]
        [SerializeField] bool hideOnStart = true;

        [Tooltip("Пауза перед появлением HUD, секунды")]
        [SerializeField] float firstDelay = 0.5f;

        [Tooltip("Пауза между появлением HUD и подсказкой управления, секунды")]
        [SerializeField] float stepDelay = 1.2f;

        [Tooltip("Звук появления элемента HUD (может быть пустым)")]
        [SerializeField] AudioClip appearClip;

        /// <summary>Онбординг сейчас идёт.</summary>
        public bool IsPlaying { get; private set; }

        // Онбординг уже пройден (сохраняется SaveSystem): после загрузки сохранения HUD в Start не прячем.
        bool onboarded;

        // Части, которые прячутся до онбординга. Карточка задания не входит: её показывает ObjectiveUI,
        // когда задание выдано, а первое задание появляется уже после онбординга.
        static readonly HudPart[] Parts = { HudPart.Sanity, HudPart.Heartbeat, HudPart.Phone };

        IEnumerator Start()
        {
            if (!hideOnStart || onboarded) yield break;
            // При смене сцены UIManager сбрасывает видимость HUD (activeSceneChanged). Прячем в Start
            // и ещё раз кадром позже — так порядок событий загрузки сцены не важен.
            HideAll();
            yield return null;
            if (!IsPlaying && !onboarded) HideAll();
        }

        /// <summary>Мгновенно прячет рассудок, пульс и телефон.</summary>
        public void HideAll()
        {
            if (UIManager.Instance == null) return;
            foreach (var part in Parts) UIManager.Instance.SetHudVisible(part, false);
        }

        /// <summary>
        /// Проигрывает онбординг: рассудок, пульс и телефон разом → подсказка управления (ждёт закрытия) → done.
        /// Дэнни стоит всё это время; управление возвращается, только если его заблокировал сам онбординг.
        /// </summary>
        public void Play(Action done = null)
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[Story] HudOnboarding уже идёт");
                return;
            }
            StartCoroutine(Run(done));
        }

        IEnumerator Run(Action done)
        {
            IsPlaying = true;
            Debug.Log("[Story] HUD onboarding");
            var ui = UIManager.Instance;
            var player = PlayerController.Instance;
            bool locked = player != null && player.State == PlayerState.Normal;
            if (locked) player.SetInputLocked(true);

            HideAll();
            yield return new WaitForSeconds(firstDelay);

            // Рассудок, пульс и телефон появляются одновременно.
            if (ui != null)
                foreach (var part in Parts) ui.SetHudVisible(part, true);
            PlayAppear();
            yield return new WaitForSeconds(stepDelay);

            if (ui != null) yield return ScriptedSequence.WaitCallback(cb => ui.ShowControlsHint(cb));

            if (locked && PlayerController.Instance != null) PlayerController.Instance.SetInputLocked(false);
            IsPlaying = false;
            onboarded = true;
            done?.Invoke();
        }

        void PlayAppear()
        {
            if (appearClip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfx(appearClip);
        }
    }
}
