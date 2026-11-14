using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Помехи на экране (H3: помехи на мониторе, сквозь них на миг проступает «DD»).
    /// Заглушка: сетка серых квадратов, которые случайно мерцают поверх экрана.
    /// </summary>
    public class StaticNoiseVisual : HallucinationVisual
    {
        [Tooltip("Сколько секунд идут помехи")]
        [SerializeField] float duration = 1.6f;
        [Tooltip("Сетка квадратов: столбцы × строки")]
        [SerializeField] Vector2Int grid = new Vector2Int(16, 9);
        [Tooltip("Непрозрачность помех")]
        [Range(0f, 1f)] [SerializeField] float alpha = 0.55f;
        [Tooltip("Смена кадра помех, раз в секунду")]
        [SerializeField] float fps = 20f;

        [Header("Надпись сквозь помехи")]
        [Tooltip("Текст, который на миг проступает (пусто — без надписи)")]
        [SerializeField] string label = "";
        [Tooltip("Через сколько секунд от начала проступает надпись")]
        [SerializeField] float labelAt = 0.7f;
        [Tooltip("Сколько секунд видна надпись")]
        [SerializeField] float labelTime = 0.15f;

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
            var root = OverlaySquare("Static", Color.clear);
            Stretch(root.rectTransform);
            var cells = new Image[grid.x * grid.y];
            for (int y = 0; y < grid.y; y++)
            for (int x = 0; x < grid.x; x++)
            {
                var cell = new GameObject("Cell", typeof(RectTransform)).AddComponent<Image>();
                cell.raycastTarget = false;
                var r = cell.rectTransform;
                r.SetParent(root.rectTransform, false);
                r.anchorMin = new Vector2((float)x / grid.x, (float)y / grid.y);
                r.anchorMax = new Vector2((float)(x + 1) / grid.x, (float)(y + 1) / grid.y);
                r.offsetMin = r.offsetMax = Vector2.zero;
                cells[y * grid.x + x] = cell;
            }

            TextMeshProUGUI text = null;
            if (!string.IsNullOrEmpty(label))
            {
                text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                text.rectTransform.SetParent(root.rectTransform, false);
                Stretch(text.rectTransform);
                text.text = label;
                text.fontSize = 220f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.raycastTarget = false;
                text.enabled = false;
            }

            SeenIfNeeded(hallucination);
            HitIfNeeded(hallucination);
            float nextFrame = 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (t >= nextFrame)
                {
                    nextFrame = t + 1f / Mathf.Max(1f, fps);
                    foreach (var cell in cells)
                    {
                        float v = Random.value;
                        cell.color = new Color(v, v, v, alpha * Random.Range(0.3f, 1f));
                    }
                }
                if (text != null) text.enabled = t >= labelAt && t < labelAt + labelTime;
                yield return null;
            }
            Destroy(root.gameObject);
        }
    }
}
