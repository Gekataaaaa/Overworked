using TMPro;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Подсказка над головой Дэнни, какой кнопкой пользоваться фонариком («[F] Turn the flashlight on / off»,
    /// как в нарративе, сцена 2.0). Show() — после подбора фонарика; прячется, когда фонарик включили,
    /// или через hideAfter секунд. Шрифт и обводка — как у подсказки [E] (InteractionHighlight).
    /// </summary>
    public class FlashlightHint : MonoBehaviour
    {
        [Tooltip("Текст подсказки (английский)")]
        [SerializeField] string text = "[F] Turn the flashlight on / off";

        [Tooltip("Через сколько секунд спрятать, если фонарик так и не включили")]
        [SerializeField] float hideAfter = 10f;

        [Tooltip("Высота над центром Дэнни, юнитов")]
        [SerializeField] float height = 1.6f;

        TextMeshPro label;
        float shownAt = -1f;
        bool subscribed;

        /// <summary>Показать подсказку над Дэнни.</summary>
        public void Show()
        {
            if (label == null) Build();
            label.text = text;
            label.gameObject.SetActive(true);
            shownAt = Time.time;
            if (!subscribed && FlashlightController.Instance != null)
            {
                FlashlightController.Instance.OnToggled += OnToggled;
                subscribed = true;
            }
        }

        void OnToggled(bool on)
        {
            if (on) Hide();
        }

        void Hide()
        {
            if (label != null) label.gameObject.SetActive(false);
            shownAt = -1f;
        }

        void LateUpdate()
        {
            if (shownAt < 0f || label == null) return;
            if (Time.time - shownAt > hideAfter) { Hide(); return; }
            var player = PlayerController.Instance;
            if (player != null) label.transform.position = player.transform.position + Vector3.up * height;
        }

        void OnDestroy()
        {
            if (subscribed && FlashlightController.Instance != null) FlashlightController.Instance.OnToggled -= OnToggled;
        }

        void Build()
        {
            var go = new GameObject("FlashlightHintLabel");
            go.transform.SetParent(transform, false);
            label = go.AddComponent<TextMeshPro>();
            label.fontSize = 3f;
            label.alignment = TextAlignmentOptions.Bottom;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(10f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.sortingOrder = 100;
            // Тёмная обводка букв — читается и в темноте Floor_02.
            label.fontMaterial.EnableKeyword("OUTLINE_ON");
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(15, 15, 15, 255);
            go.SetActive(false);
        }
    }
}
