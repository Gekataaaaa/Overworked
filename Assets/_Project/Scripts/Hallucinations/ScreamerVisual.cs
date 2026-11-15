using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Скример: фигура бросается в камеру. С кадрами (frames — полноэкранная анимация двойника,
    /// Art/Characters/Screamer/Screamer_FullScreen) они проигрываются на весь экран, удар — на последнем кадре
    /// (лицо во весь экран), он держится с тряской и гаснет. Без кадров — квадрат-заглушка, который из центра
    /// экрана за доли секунды вырастает до размеров экрана. В начале броска силуэты этой галлюцинации
    /// (двойник, который шёл к Дэнни) прячутся. В момент удара экран накрывает сильный шум (телевизионный
    /// «снег»): держится noiseHold секунд и плавно уходит за noiseFade — всего около секунды, раньше конца броска.
    /// </summary>
    public class ScreamerVisual : HallucinationVisual
    {
        [Tooltip("Полноэкранная анимация броска (1920×1080, по порядку). Пусто — квадрат-заглушка")]
        [SerializeField] Sprite[] frames;
        [Tooltip("Скорость анимации броска, кадров в секунду")]
        [SerializeField] float framesFps = 14f;

        [Tooltip("Цвет квадрата-заглушки (если кадров нет)")]
        [SerializeField] Color color = new Color(0.45f, 0.03f, 0.05f);
        [Tooltip("Начальный размер в долях экрана")]
        [Range(0.01f, 1f)] [SerializeField] float startSize = 0.12f;
        [Tooltip("Бросок: за сколько секунд квадрат заполняет экран")]
        [SerializeField] float lungeTime = 0.22f;
        [Tooltip("Сколько секунд держится на весь экран")]
        [SerializeField] float holdTime = 0.8f;
        [Tooltip("Тряска на пике, доля экрана")]
        [SerializeField] float shake = 0.03f;
        [Tooltip("Исчезновение, секунд (0 — обрывается)")]
        [SerializeField] float fadeOut = 0.1f;

        [Header("Шум на пике")]
        [Tooltip("Накрыть экран шумом в момент удара")]
        [SerializeField] bool noise = true;
        [Tooltip("Сколько секунд шум на полной силе")]
        [SerializeField] float noiseHold = 0.45f;
        [Tooltip("За сколько секунд шум плавно уходит")]
        [SerializeField] float noiseFade = 0.55f;
        [Tooltip("Сила шума (непрозрачность) на пике: меньше 1 — сквозь него видно лицо")]
        [Range(0f, 1f)] [SerializeField] float noiseAlpha = 0.75f;
        [Tooltip("Смена кадров шума в секунду")]
        [SerializeField] float noiseFps = 30f;
        [Tooltip("Шипение шума (Audio/Horror/SFX_StaticNoise — шум заставки Tiny UFO). Пусто — без звука")]
        [SerializeField] AudioClip noiseSound;
        [Tooltip("Громкость шипения на пике 0..1; уходит вместе с картинкой шума")]
        [Range(0f, 1f)] [SerializeField] float noiseVolume = 0.45f;

        // Несколько готовых кадров шума на всю игру: генерировать каждый кадр — дорого.
        const int NoiseW = 320, NoiseH = 180, NoiseVariants = 4;
        static Texture2D[] noiseTextures;
        RawImage noiseImage;

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
            // Бросок начался — двойник, который шёл к Дэнни, исчезает: теперь он летит в экран.
            foreach (var silhouette in hallucination.GetComponentsInChildren<SilhouetteBehaviour>(true))
                silhouette.Cleanup();

            if (frames != null && frames.Length > 0)
            {
                yield return PlayFrames(hallucination);
                yield break;
            }
            var image = OverlaySquare("ScreamerSquare", color);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var canvas = (RectTransform)rect.parent;

            // Квадрат: сторона — от длинной стороны экрана, на пике с запасом, чтобы тряска не открывала края.
            float Full() => Mathf.Max(canvas.rect.width, canvas.rect.height) * 1.2f;

            for (float t = 0f; t < lungeTime; t += Time.deltaTime)
            {
                // Ускорение к концу: фигура «летит» в камеру.
                float k = t / lungeTime;
                rect.sizeDelta = Vector2.one * Full() * Mathf.Lerp(startSize, 1f, k * k * k);
                yield return null;
            }

            SeenIfNeeded(hallucination);
            HitIfNeeded(hallucination);
            StartNoise(hallucination);
            for (float t = 0f; t < holdTime; t += Time.deltaTime)
            {
                rect.sizeDelta = Vector2.one * Full();
                rect.anchoredPosition = Random.insideUnitCircle * shake * Full();
                yield return null;
            }

            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                var c = color;
                c.a = color.a * (1f - t / fadeOut);
                image.color = c;
                yield return null;
            }
            Destroy(image.gameObject);
        }

        public override void Cleanup()
        {
            if (noiseImage != null) Destroy(noiseImage.gameObject);
        }

        // Корутина на объекте галлюцинации: шум не должен оборваться, если бросок закончится раньше него.
        void StartNoise(HallucinationEvent hallucination)
        {
            if (!noise || hallucination == null) return;
            hallucination.StartCoroutine(Noise());
        }

        IEnumerator Noise()
        {
            var textures = NoiseTextures();
            noiseImage = new GameObject("ScreamerNoise", typeof(RectTransform)).AddComponent<RawImage>();
            noiseImage.raycastTarget = false;
            var rect = noiseImage.rectTransform;
            rect.SetParent(Overlay(), false);
            Stretch(rect);
            rect.SetAsLastSibling();   // поверх кадров броска

            // Шипение идёт вместе с картинкой шума: та же сила, тот же спад.
            var audio = AudioManager.Instance;
            var hiss = audio != null ? audio.PlaySfxHeld(noiseSound, noiseVolume, true) : null;
            float frame = 1f / Mathf.Max(1f, noiseFps), next = 0f;
            for (float t = 0f; t < noiseHold + noiseFade; t += Time.deltaTime)
            {
                if (noiseImage == null) break;
                if (t >= next)
                {
                    next = t + frame;
                    noiseImage.texture = textures[Random.Range(0, textures.Length)];
                    // Случайный сдвиг кадра шума — снег «бежит», а не повторяется.
                    noiseImage.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
                }
                // На пике — полная сила с лёгким дрожанием, потом плавный спад (SmoothStep — мягко в конце).
                float k = t < noiseHold ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (t - noiseHold) / noiseFade);
                float a = noiseAlpha * k * (t < noiseHold ? Random.Range(0.85f, 1f) : 1f);
                noiseImage.color = new Color(1f, 1f, 1f, a);
                if (audio != null) audio.SetSfxVolume(hiss, noiseVolume * k);
                yield return null;
            }
            if (audio != null) audio.StopSfx(hiss, 0.05f);
            if (noiseImage != null) Destroy(noiseImage.gameObject);
        }

        // Серый «снег» с редкими светлыми и тёмными горизонтальными полосами, без сглаживания (Point).
        static Texture2D[] NoiseTextures()
        {
            if (noiseTextures != null && noiseTextures[0] != null) return noiseTextures;
            noiseTextures = new Texture2D[NoiseVariants];
            var px = new Color32[NoiseW * NoiseH];
            for (int n = 0; n < NoiseVariants; n++)
            {
                for (int y = 0; y < NoiseH; y++)
                {
                    float band = Random.value < 0.04f ? Random.Range(-0.35f, 0.45f) : 0f;
                    for (int x = 0; x < NoiseW; x++)
                    {
                        byte v = (byte)(Mathf.Clamp01(Random.value + band) * 255f);
                        px[y * NoiseW + x] = new Color32(v, v, v, 255);
                    }
                }
                var tex = new Texture2D(NoiseW, NoiseH, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat,
                    name = "ScreamerNoise_" + n,
                };
                tex.SetPixels32(px);
                tex.Apply(false, true);
                noiseTextures[n] = tex;
            }
            return noiseTextures;
        }

        IEnumerator PlayFrames(HallucinationEvent hallucination)
        {
            var image = OverlaySquare("ScreamerFrames", Color.white);
            var rect = image.rectTransform;
            Stretch(rect);
            // С запасом за края экрана: тряска на пике не открывает сцену по бокам.
            rect.offsetMin = new Vector2(-40f, -40f);
            rect.offsetMax = new Vector2(40f, 40f);
            image.preserveAspect = false;

            // Бросок: кадры по порядку (от фигуры вдали до лица во весь экран).
            float frameTime = 1f / Mathf.Max(1f, framesFps);
            for (int i = 0; i < frames.Length; i++)
            {
                image.sprite = frames[i];
                for (float t = 0f; t < frameTime; t += Time.deltaTime) yield return null;
            }

            // Пик — последний кадр: удар (звук, пульс), лицо держится и трясётся.
            SeenIfNeeded(hallucination);
            HitIfNeeded(hallucination);
            StartNoise(hallucination);
            var canvas = (RectTransform)rect.parent;
            float size = Mathf.Max(canvas.rect.width, canvas.rect.height);
            for (float t = 0f; t < holdTime; t += Time.deltaTime)
            {
                rect.anchoredPosition = Random.insideUnitCircle * shake * size;
                yield return null;
            }
            rect.anchoredPosition = Vector2.zero;

            for (float t = 0f; t < fadeOut; t += Time.deltaTime)
            {
                image.color = new Color(1f, 1f, 1f, 1f - t / fadeOut);
                yield return null;
            }
            Destroy(image.gameObject);
        }
    }
}
