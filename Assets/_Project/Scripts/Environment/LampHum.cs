using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Звуки ламп — только тех, что в кадре камеры (LampLight): лампа вне кадра не слышна.
    /// Гул (Audio/Ambient/Resources/AMB_LampBuzz) звучит фоном, пока в кадре горит лампа; громкость идёт за самой
    /// яркой лампой в кадре — ушли из кадра или погасли, гул плавно стихает, мигнула — проседает вместе с ней.
    /// Треск (AMB_LampFlicker) — пока мигает хоть одна лампа в кадре (LampLight.IsFlickering), быстро гаснет после.
    /// Создаёт себя сам на всю игру. Гул зациклен без провала (SeamlessLoop).
    /// </summary>
    public class LampHum : MonoBehaviour
    {
        const string HumName = "AMB_LampBuzz";
        const string FlickerName = "AMB_LampFlicker";

        [Header("Гул")]
        [Tooltip("Громкость гула, когда в кадре горит лампа (0..1, умножается на канал SFX)")]
        [Range(0f, 1f)] [SerializeField] float volume = 0.3f;

        [Tooltip("Скорость изменения громкости гула: долей полной громкости в секунду")]
        [SerializeField] float fadeSpeed = 4f;

        [Header("Треск мигания")]
        [Tooltip("Громкость треска, пока лампа в кадре мигает (0..1)")]
        [Range(0f, 1f)] [SerializeField] float flickerVolume = 0.6f;

        [Tooltip("Как быстро треск появляется, долей в секунду (мигание — резко)")]
        [SerializeField] float flickerFadeIn = 25f;

        [Tooltip("Как быстро треск стихает после мигания, долей в секунду")]
        [SerializeField] float flickerFadeOut = 6f;

        [Header("Кадр")]
        [Tooltip("Запас за краем кадра (доля экрана): лампа чуть за краем ещё слышна")]
        [SerializeField] float viewMargin = 0.1f;

        SeamlessLoop hum;
        AudioClip flicker;
        AudioSource flickerSource;
        float level, flickerLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (FindAnyObjectByType<LampHum>() != null) return;
            var go = new GameObject("LampHum");
            DontDestroyOnLoad(go);
            go.AddComponent<LampHum>();
        }

        void Awake()
        {
            var humClip = Resources.Load<AudioClip>(HumName);
            hum = new SeamlessLoop(humClip);
            flicker = Resources.Load<AudioClip>(FlickerName);
            if (humClip == null) Debug.LogWarning($"[Audio] нет Resources/{HumName} — лампы не гудят");
            if (flicker == null) Debug.LogWarning($"[Audio] нет Resources/{FlickerName} — мигание без треска");
        }

        void Update()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            VisibleLamps(out float bright, out bool flickering);
            level = Mathf.MoveTowards(level, bright, fadeSpeed * Time.unscaledDeltaTime);
            hum.Update(volume * level);
            UpdateFlicker(audio, flickering);
        }

        void UpdateFlicker(AudioManager audio, bool flickering)
        {
            if (flicker == null) return;
            float speed = flickering ? flickerFadeIn : flickerFadeOut;
            flickerLevel = Mathf.MoveTowards(flickerLevel, flickering ? 1f : 0f, speed * Time.unscaledDeltaTime);
            if (flickerLevel <= 0f)
            {
                if (flickerSource != null) audio.StopSfx(flickerSource, 0f);
                flickerSource = null;
                return;
            }
            if (flickerSource == null)
            {
                flickerSource = audio.PlaySfxHeld(flicker, 0f, true);
                // Каждое мигание — с другого места записи, чтобы треск не повторялся одинаково.
                if (flickerSource != null) flickerSource.time = Random.Range(0f, flicker.length * 0.8f);
            }
            audio.SetSfxVolume(flickerSource, flickerVolume * flickerLevel);
        }

        // Лампы в кадре: самая яркая (0 — ни одной или камеры нет — главное меню) и мигает ли хоть одна.
        void VisibleLamps(out float bright, out bool flickering)
        {
            bright = 0f;
            flickering = false;
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var lamp in LampLight.Active)
            {
                if (lamp == null || !InView(cam, lamp.transform.position, viewMargin)) continue;
                bright = Mathf.Max(bright, lamp.Level);
                flickering |= lamp.IsFlickering;
            }
        }

        /// <summary>Точка в кадре камеры с запасом margin (доля экрана) за краями.</summary>
        public static bool InView(Camera cam, Vector3 world, float margin)
        {
            Vector3 v = cam.WorldToViewportPoint(world);
            return v.z >= 0f && v.x >= -margin && v.x <= 1f + margin && v.y >= -margin && v.y <= 1f + margin;
        }
    }
}
