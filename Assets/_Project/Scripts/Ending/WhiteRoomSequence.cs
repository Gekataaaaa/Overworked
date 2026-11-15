using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Сцена WhiteRoom целиком (паспорт, раздел 10): белое пространство, HUD скрыт, полная тишина — слышны только
    /// шаги Дэнни (удары сердца и звук печати субтитров заглушены), Дэнни идёт только вправо и говорит с игроком
    /// (реплики — на WhiteRoomTrigger по пути). У кровати: Дэнни ложится, пульс (неслышно) равномерно гаснет до нуля,
    /// под него — писк кардиомонитора на каждый удар, затем непрерывный сигнал, экран растворяется в белом → титры → MainMenu.
    /// Лежит на объекте WhiteRoom в сцене; собирается меню Overworked → P4 → Build White Room.
    /// </summary>
    public class WhiteRoomSequence : MonoBehaviour
    {
        [Header("Вход")]
        [Tooltip("Сколько секунд Дэнни молчит после входа, прежде чем заговорить")]
        [SerializeField] float introDelay = 3f;
        [Tooltip("Первые реплики: «{name}… Are you still here?»")]
        [SerializeField] DialogueData introLines;
        [Tooltip("ВРЕМЕННО для проверки: не прятать шкалу рассудка и пульс HUD (P3). По паспорту в WhiteRoom HUD скрыт — перед финалом выключить")]
        [SerializeField] bool debugShowHorrorHud;

        [Header("Кровать")]
        [Tooltip("Куда ложится Дэнни (точка на кровати)")]
        [SerializeField] Transform bedPoint;
        [Tooltip("За сколько секунд пульс гаснет до нуля, когда Дэнни лёг")]
        [SerializeField] float heartFadeTime = 14f;
        [Tooltip("Через сколько секунд после того, как Дэнни лёг, проявляются звуки ИВЛ и кардиомонитора")]
        [SerializeField] float hospitalSoundsDelay = 2.5f;

        [Header("Финал")]
        [Tooltip("Сколько секунд звучит непрерывный сигнал, прежде чем экран начнёт белеть")]
        [SerializeField] float flatlineBeforeWhite = 2f;
        [Tooltip("За сколько секунд экран растворяется в белом")]
        [SerializeField] float whiteFadeTime = 4f;
        [Tooltip("Белый слой поверх сцены (под HUD и затемнением _Systems)")]
        [SerializeField] Image whiteOverlay;
        [Tooltip("Титры: группа на белом экране, проявляется после растворения")]
        [SerializeField] CanvasGroup credits;
        [Tooltip("Текст титров")]
        [SerializeField] TextMeshProUGUI creditsText;
        [Tooltip("Строки титров (ассет в Data/Dialogues): каждая строка — с новой строки")]
        [SerializeField] DialogueData creditsLines;
        [Tooltip("Сколько секунд титры на экране; любая клавиша после половины времени — сразу в меню")]
        [SerializeField] float creditsTime = 20f;
        [Tooltip("Минимальный размер шрифта титров: длинные титры уменьшаются до него, чтобы влезть в экран целиком")]
        [SerializeField] float creditsMinFontSize = 14f;

        [Header("Звуки (пусто — простые сгенерированные заглушки)")]
        [Tooltip("Один писк кардиомонитора — звучит на каждый удар сердца")]
        [SerializeField] AudioClip monitorBeep;
        [Tooltip("Зацикленный звук аппарата ИВЛ (канал Ambient). Пусто — без ИВЛ: сгенерированная заглушка шумела, её убрали")]
        [SerializeField] AudioClip ventilatorLoop;
        [Tooltip("Длинный непрерывный сигнал кардиомонитора (зациклен)")]
        [SerializeField] AudioClip flatlineLoop;
        [Tooltip("Громкость писка монитора 0..1")]
        [Range(0f, 1f)] [SerializeField] float beepVolume = 0.35f;
        [Tooltip("Громкость непрерывного сигнала 0..1")]
        [Range(0f, 1f)] [SerializeField] float flatlineVolume = 0.35f;

        /// <summary>Дэнни уже лёг — дальше всё идёт само.</summary>
        public bool InBed { get; private set; }

        AudioSource flatline;
        bool beeping;
        // Вступление уже прозвучало (сохраняется SaveSystem) — после загрузки сохранения не повторяем.
        bool introDone;

        void Awake()
        {
            if (monitorBeep == null) monitorBeep = HospitalSounds.Beep();
            if (flatlineLoop == null) flatlineLoop = HospitalSounds.Flatline();

            flatline = gameObject.AddComponent<AudioSource>();
            flatline.playOnAwake = false;
            flatline.loop = true;
            flatline.spatialBlend = 0f;
            flatline.clip = flatlineLoop;

            SetAlpha(whiteOverlay, 0f);
            if (credits != null) credits.alpha = 0f;
        }

        IEnumerator Start()
        {
            // Со скримера могли остаться фон и музыка — здесь слышен только пульс.
            if (AudioManager.Instance != null) AudioManager.Instance.StopAll();
            if (HeartbeatManager.Instance != null) HeartbeatManager.Instance.EnterWhiteRoom();
            // Полная тишина: только шаги, писк монитора в конце. Сердце считается дальше (по нему пищит монитор),
            // но его удары не слышны; субтитры печатаются беззвучно.
            SetHeartbeatSoundMuted(true);
            UiSounds.MuteTyping = true;
            if (UIManager.Instance != null)
                foreach (HudPart part in Enum.GetValues(typeof(HudPart)))
                {
                    bool horror = part == HudPart.Sanity || part == HudPart.Heartbeat;
                    UIManager.Instance.SetHudVisible(part, debugShowHorrorHud && horror);
                }

            var player = PlayerController.Instance;
            if (player != null)
            {
                player.SetOnlyRight(true);
                player.SetInputLocked(true);
            }

            // Ждём, пока экран проявится после загрузки, — иначе Дэнни уйдёт, пока игрок ещё не видит сцену.
            while (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsLoading)
                yield return null;
            if (player != null && !InBed) player.SetInputLocked(false);
            if (player == null) Debug.LogWarning("[WhiteRoom] в сцене нет PlayerController — идти некому");
            // На паузе (timeScale 0) FixedUpdate не идёт — Дэнни стоит, реплики не наступают.
            if (Time.timeScale <= 0f) Debug.LogWarning("[WhiteRoom] Time.timeScale = 0 — игра на паузе, Дэнни не пойдёт");

            yield return new WaitForSeconds(introDelay);
            if (!InBed && !introDone)
            {
                introDone = true;
                Say(introLines);
            }
        }

        /// <summary>Показать реплики субтитрами (DialogueManager, P3).</summary>
        public void Say(DialogueData lines)
        {
            if (lines != null && DialogueManager.Instance != null) DialogueManager.Instance.Play(lines);
        }

        /// <summary>Дэнни дошёл до кровати: ложится автоматически, дальше финал без участия игрока.</summary>
        public void LieDown()
        {
            if (InBed) return;
            InBed = true;
            StartCoroutine(Ending());
        }

        IEnumerator Ending()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.SetInputLocked(true);
                if (bedPoint != null) player.Teleport(bedPoint);
                player.PlayAnim("LieDown");
                PlaceholderLieDown(player);
            }

            var heart = HeartbeatManager.Instance;
            if (heart != null) heart.FadeBpm(0f, heartFadeTime);
            float heartStopsAt = Time.time + heartFadeTime;

            yield return new WaitForSeconds(hospitalSoundsDelay);
            // Палата проявляется постепенно: монитор пищит в такт гаснущему сердцу (+ ИВЛ, если назначен настоящий звук).
            if (ventilatorLoop != null && AudioManager.Instance != null) AudioManager.Instance.PlayAmbient(ventilatorLoop);
            if (heart != null) { heart.OnBeat += Beep; beeping = true; }

            while (Time.time < heartStopsAt || (heart != null && heart.Bpm > 0f))
                yield return null;
            StopBeeping();

            flatline.volume = flatlineVolume;
            flatline.Play();
            yield return new WaitForSeconds(flatlineBeforeWhite);

            yield return FadeImage(whiteOverlay, 1f, whiteFadeTime);
            // На полностью белом экране звуки палаты гаснут — остаётся тишина и титры.
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAmbient(null);
            yield return FadeVolume(flatline, 0f, 2f);
            flatline.Stop();

            yield return ShowCredits();
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }

        IEnumerator ShowCredits()
        {
            if (credits == null) yield break;
            if (creditsText != null && creditsLines != null && creditsLines.Lines != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var line in creditsLines.Lines) sb.AppendLine(TextFormatter.Format(line.Text));
                creditsText.text = sb.ToString();
            }

            credits.alpha = 0f;

            // Все титры — на одном экране: шрифт сам уменьшается, пока строки не влезут в рамку (без прокрутки).
            if (creditsText != null)
            {
                // Максимум — размер из сцены (34): короткие титры остаются крупными.
                if (!creditsText.enableAutoSizing) creditsText.fontSizeMax = creditsText.fontSize;
                creditsText.enableAutoSizing = true;
                creditsText.fontSizeMin = creditsMinFontSize;
                creditsText.ForceMeshUpdate();
            }
            float duration = creditsTime;

            // Титры проявляются за 2 с.
            float start = Time.time;
            while (Time.time - start < duration)
            {
                credits.alpha = Mathf.Clamp01((Time.time - start) / 2f);
                // Пропуск — только после половины: случайно зажатая клавиша не обрывает титры сразу.
                if (Time.time - start > duration / 2f && AnyKeyPressed()) break;
                yield return null;
            }

            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                credits.alpha = 1f - t / 1.5f;
                yield return null;
            }
            credits.alpha = 0f;
        }

        static bool AnyKeyPressed()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        /// <summary>
        /// Пока у Дэнни нет анимации LieDown, спрайт-заглушку просто кладём на бок.
        /// Как только появится Animator с контроллером — этот поворот не делается.
        /// </summary>
        static void PlaceholderLieDown(PlayerController player)
        {
            var animator = player.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null) return;
            var sprite = player.GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) return;
            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            sprite.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        }

        void Beep()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(monitorBeep, beepVolume);
        }

        void StopBeeping()
        {
            if (!beeping) return;
            beeping = false;
            if (HeartbeatManager.Instance != null) HeartbeatManager.Instance.OnBeat -= Beep;
        }

        void OnDestroy()
        {
            StopBeeping();
            // Уходим в меню: пульс и рассудок — как при первом запуске, иначе новое прохождение
            // начнётся с остановленным сердцем (сценарный режим на нуле).
            if (HeartbeatManager.Instance != null) HeartbeatManager.Instance.SetScripted(false);
            if (SanityManager.Instance != null) SanityManager.Instance.SetSanityInstant(100f);
            if (PlayerController.Instance != null) PlayerController.Instance.SetOnlyRight(false);
            SetHeartbeatSoundMuted(false);
            UiSounds.MuteTyping = false;
        }

        /// <summary>
        /// Глушит канал сердцебиения AudioManager (его AudioSource «Audio_Heartbeat»).
        /// Без правок AudioManager / HeartbeatManager: у них нет API «тихо, но считать дальше».
        /// </summary>
        static void SetHeartbeatSoundMuted(bool muted)
        {
            if (AudioManager.Instance == null) return;
            var channel = AudioManager.Instance.transform.Find("Audio_Heartbeat");
            var source = channel != null ? channel.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                if (muted) Debug.LogWarning("[WhiteRoom] не найден канал Audio_Heartbeat — удары сердца не заглушены");
                return;
            }
            source.mute = muted;
        }

        static IEnumerator FadeImage(Image img, float target, float seconds)
        {
            if (img == null) yield break;
            float start = img.color.a;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                // Smoothstep: «медленно растворяется» — без резкого старта и обрыва.
                SetAlpha(img, Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            SetAlpha(img, target);
        }

        static IEnumerator FadeVolume(AudioSource src, float target, float seconds)
        {
            float start = src.volume;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                src.volume = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            src.volume = target;
        }

        static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
