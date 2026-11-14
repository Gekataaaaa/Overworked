using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Overworked
{
    /// <summary>
    /// Лампа на потолке: настоящий 2D-свет (Light2D) и её нарисованные части — трубка и конус света.
    /// Картинки повторяют яркость света: когда галлюцинация (LightFlickerVisual P4) или мерцание
    /// гасит/вспыхивает Light2D, трубка темнеет, а конус тает и проявляется вместе с ним.
    /// Так «лампы мигают» по-настоящему, а не только вспышка на экране.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LampLight : MonoBehaviour
    {
        [Tooltip("Трубка лампы (спрайт Light_Tube_On): темнеет вместе со светом")]
        [SerializeField] SpriteRenderer tube;

        [Tooltip("Конус света под лампой (спрайт Light_Cone): прозрачнеет вместе со светом")]
        [SerializeField] SpriteRenderer cone;

        [Tooltip("Яркость трубки, когда свет полностью погас (доля обычной)")]
        [Range(0f, 1f)] [SerializeField] float tubeDarkLevel = 0.25f;

        /// <summary>Трубка лампы (её яркостью управляет сама лампа).</summary>
        public SpriteRenderer Tube => tube;

        /// <summary>Конус света лампы.</summary>
        public SpriteRenderer Cone => cone;

        Light2D lamp;
        float baseIntensity;
        Color tubeColor, coneColor;

        /// <summary>
        /// Насколько лампа сейчас горит: 1 — как обычно, 0 — погасла (вспышка ярче обычного — тоже 1).
        /// По ней BackgroundDimmer затемняет фон.
        /// </summary>
        public float Level
        {
            get
            {
                if (lamp == null) return 1f;
                return lamp.enabled ? Mathf.Clamp01(lamp.intensity / baseIntensity) : 0f;
            }
        }

        void Awake()
        {
            lamp = GetComponent<Light2D>();
            baseIntensity = Mathf.Max(0.0001f, lamp.intensity);
            if (tube != null) tubeColor = tube.color;
            if (cone != null) coneColor = cone.color;
        }

        // В LateUpdate: мерцание P4 меняет intensity в Update/корутине этого же кадра.
        void LateUpdate()
        {
            float k = lamp.enabled ? lamp.intensity / baseIntensity : 0f;
            if (tube != null)
            {
                float t = Mathf.Lerp(tubeDarkLevel, 1f, Mathf.Clamp01(k));
                tube.color = new Color(tubeColor.r * t, tubeColor.g * t, tubeColor.b * t, tubeColor.a);
            }
            if (cone != null)
            {
                // Вспышка ярче обычного (k > 1) — конус чуть плотнее, но не больше 1.
                float a = Mathf.Clamp01(coneColor.a * Mathf.Clamp(k, 0f, 1.3f));
                cone.color = new Color(coneColor.r, coneColor.g, coneColor.b, a);
            }
        }
    }
}
