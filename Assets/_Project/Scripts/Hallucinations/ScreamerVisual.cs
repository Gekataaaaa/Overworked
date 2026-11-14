using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Скример: фигура бросается в камеру. Сейчас — квадрат, который из центра экрана за доли секунды
    /// вырастает до размеров экрана, трясётся на пике и исчезает. Удар — в момент, когда экран заполнен.
    /// </summary>
    public class ScreamerVisual : HallucinationVisual
    {
        [Tooltip("Цвет квадрата-заглушки (потом — спрайт двойника Дэнни)")]
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

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
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
    }
}
