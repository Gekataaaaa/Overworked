using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Общие помощники для сборщиков UI (MainMenuBuilder, PauseMenuBuilder): объекты с RectTransform,
    /// тексты TMP, кнопки меню, заполнение приватных [SerializeField]-полей.
    /// </summary>
    internal static class UiBuild
    {
        /// <summary>Цвет текста меню (светло-серый).</summary>
        internal static readonly Color MenuText = new Color(0.78f, 0.8f, 0.76f);

        internal static Button MenuButton(string name, RectTransform parent, TMP_FontAsset font, string label)
        {
            var rt = UI(name, parent);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.14f, 0.13f);
            colors.highlightedColor = colors.selectedColor = new Color(0.24f, 0.29f, 0.26f);
            colors.pressedColor = new Color(0.33f, 0.4f, 0.36f);
            btn.colors = colors;
            var t = Tmp("Label", rt, font, label, 44, MenuText, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            return btn;
        }

        // ------------------------------------------------------------ помощники UI

        internal static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static RectTransform Stretch(RectTransform rt, float margin = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        internal static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        internal static TextMeshProUGUI Tmp(string name, RectTransform parent, TMP_FontAsset font, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = UI(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Заполняет приватные [SerializeField]-поля компонента по именам.</summary>
        internal static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs) so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
