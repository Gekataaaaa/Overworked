using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: собирает плашку субтитров (объект Subtitles) внутри HUD префаба _Systems и проставляет
    /// ссылки и стили в DialogueManager. Меню Overworked → P3 → UI → Build Dialogue UI.
    /// Пересоздаёт только Subtitles, остальное в _Systems не трогает.
    /// </summary>
    public static class DialogueUiBuilder
    {
        const string SystemsPath = "Assets/_Project/Prefabs/Systems/Resources/_Systems.prefab";
        const string MainFontPath = "Assets/_Project/Fonts/PTSans SDF.asset";
        const string MonoFontPath = "Assets/_Project/Fonts/IBMPlexMono SDF.asset";

        static readonly Color DannyColor = new Color(0.93f, 0.93f, 0.88f);
        static readonly Color DescriptionColor = new Color(0.66f, 0.73f, 0.69f);
        static readonly Color SystemColor = new Color(0.55f, 0.88f, 0.55f);

        [MenuItem("Overworked/P3/UI/Build Dialogue UI")]
        public static void Run()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            var mono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoFontPath);
            if (font == null || mono == null)
            {
                Debug.LogError("[Setup] нет шрифтов PT Sans / IBM Plex Mono — сначала Overworked → P1 → UI → Build Main Menu");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPath);
            try
            {
                var hud = root.transform.Find("HUD") as RectTransform;
                var manager = root.GetComponentInChildren<DialogueManager>(true);
                if (hud == null || manager == null)
                {
                    Debug.LogError("[Setup] в _Systems нет HUD или DialogueManager");
                    return;
                }
                var old = hud.Find("Subtitles");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var rt = UI("Subtitles", hud);
                // Первым ребёнком HUD: телефон, тосты и затемнение рисуются поверх субтитров.
                rt.SetAsFirstSibling();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 50);
                rt.sizeDelta = new Vector2(1300, 130);
                var bg = rt.gameObject.AddComponent<Image>();
                bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0f, 0f, 0f, 0.55f);
                bg.raycastTarget = false;
                var group = rt.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;

                // Субтитры — пиксельный Hardpixel (P3Fonts); стиль System по-прежнему IBM Plex Mono.
                var text = Tmp("Text", rt, P3Fonts.Dialogue(), "", 38, DannyColor, TextAlignmentOptions.Center);
                Stretch(text.rectTransform, 16);
                text.textWrappingMode = TextWrappingModes.Normal;

                Wire(manager, ("group", group), ("text", text));
                var so = new SerializedObject(manager);
                var styles = so.FindProperty("styles");
                styles.arraySize = 3;
                SetStyle(styles.GetArrayElementAtIndex(0), Speaker.Danny, null, DannyColor, FontStyles.Normal);
                SetStyle(styles.GetArrayElementAtIndex(1), Speaker.Description, null, DescriptionColor, FontStyles.Italic);
                SetStyle(styles.GetArrayElementAtIndex(2), Speaker.System, mono, SystemColor, FontStyles.Normal);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPath);
                Debug.Log("[Setup] Dialogue UI: субтитры собраны в _Systems/HUD");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SetStyle(SerializedProperty p, Speaker speaker, TMP_FontAsset font, Color color, FontStyles style)
        {
            p.FindPropertyRelative("Speaker").enumValueIndex = (int)speaker;
            p.FindPropertyRelative("Font").objectReferenceValue = font;
            p.FindPropertyRelative("Color").colorValue = color;
            p.FindPropertyRelative("Style").intValue = (int)style;
        }
    }
}
