using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: собирает в HUD префаба _Systems места под шкалу рассудка и пульс под ней (их наполняет P4),
    /// окно текста на весь экран (записка) и подсказку управления; проставляет ссылки в UIManager.
    /// Меню Overworked → P3 → Build HUD. Пересоздаёт только свои объекты: SanityBar, Heartbeat,
    /// FullscreenText, ControlsHint; SanityBar и Heartbeat — только если их нет (их наполняет P4). Телефон и тост — Build Phone UI, субтитры — Build Dialogue UI.
    /// </summary>
    public static class HudBuilder
    {
        const string SystemsPath = "Assets/_Project/Prefabs/Systems/Resources/_Systems.prefab";
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";

        static readonly Color TextLight = new Color(0.86f, 0.88f, 0.84f);
        static readonly Color TextDim = new Color(0.55f, 0.6f, 0.57f);
        static readonly Color Paper = new Color(0.86f, 0.83f, 0.72f);
        static readonly Color Ink = new Color(0.12f, 0.1f, 0.09f);

        [MenuItem("Overworked/P3/Build HUD")]
        public static void Run()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[Setup] нет шрифта PT Sans — сначала Overworked → P1 → Build Main Menu");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(SystemsPath);
            try
            {
                var hud = root.transform.Find("HUD") as RectTransform;
                var ui = root.GetComponentInChildren<UIManager>(true);
                if (hud == null || ui == null)
                {
                    Debug.LogError("[Setup] в _Systems нет HUD или UIManager");
                    return;
                }
                foreach (var old in new[] { "FullscreenText", "ControlsHint" })
                {
                    var t = hud.Find(old);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }

                // SanityBar и Heartbeat наполняет P4 (SanityBarUI, HeartbeatUI со своими настройками) —
                // если они уже есть, не пересоздаём, иначе потеряются его компоненты и ссылки.
                var existingSanity = hud.Find("SanityBar");
                var existingHeartbeat = hud.Find("Heartbeat");
                var sanity = existingSanity != null ? existingSanity.gameObject : BuildSanityBar(hud);
                var heartbeat = existingHeartbeat != null ? existingHeartbeat.gameObject : BuildHeartbeat(hud, font);
                // Всплывающие окна — пиксельный Hardpixel (P3Fonts), как субтитры.
                var popupFont = P3Fonts.Dialogue();
                var fullscreen = BuildFullscreen(hud, popupFont, out var fullscreenText);
                var hint = BuildControlsHint(hud, popupFont);

                // Рассудок и пульс — сразу после субтитров (под телефоном), окна — поверх всего, кроме затемнения.
                var subtitles = hud.Find("Subtitles");
                int low = subtitles != null ? subtitles.GetSiblingIndex() + 1 : 0;
                if (existingSanity == null) sanity.transform.SetSiblingIndex(low);
                if (existingHeartbeat == null) heartbeat.transform.SetSiblingIndex(low + 1);
                fullscreen.transform.SetAsLastSibling();
                hint.transform.SetAsLastSibling();
                var fade = hud.Find("FadePanel");
                if (fade != null) fade.SetAsLastSibling();

                Wire(ui, ("sanityRoot", sanity), ("heartbeatRoot", heartbeat),
                    ("fullscreenRoot", fullscreen), ("fullscreenText", fullscreenText), ("controlsHintRoot", hint));

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPath);
                Debug.Log("[Setup] HUD: записка и подсказка управления собраны в _Systems/HUD (рассудок и пульс — только если их не было)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------ рассудок и пульс (места для P4)

        static GameObject BuildSanityBar(RectTransform hud)
        {
            var rt = UI("SanityBar", hud);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24, -40);
            rt.sizeDelta = new Vector2(380, 20);

            // Без тёмной подложки: на экране только сама полоса рассудка.
            var fill = UI("Fill", rt);
            Stretch(fill);
            var img = fill.gameObject.AddComponent<Image>();
            // Белый квадрат без скруглений: у скруглённого спрайта края прозрачные, и полоса визуально
            // начиналась правее иконки пульса. Filled-режим без спрайта не режет картинку.
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder_Square.png");
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.color = new Color(0.55f, 0.68f, 0.6f);
            img.raycastTarget = false;

            // Скрипт P4 (пока заглушка): ему останется взять Fill и вести fillAmount от SanityManager.
            rt.gameObject.AddComponent<SanityBarUI>();
            return rt.gameObject;
        }

        static GameObject BuildHeartbeat(RectTransform hud, TMP_FontAsset font)
        {
            // Под шкалой рассудка (левый верх): шкала на y=-40 высотой 26, пульс — ниже с зазором.
            var rt = UI("Heartbeat", hud);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24, -72);
            rt.sizeDelta = new Vector2(260, 48);
            // Без тёмной подложки: иконка и текст прямо поверх сцены, левый край — по краю шкалы.

            var icon = UI("Icon", rt);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(34, 34);
            var iimg = icon.gameObject.AddComponent<Image>();
            iimg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            iimg.color = new Color(0.8f, 0.22f, 0.2f);
            iimg.raycastTarget = false;

            // В одну строку справа от иконки: с переносом «72 BPM» ломалось на две строки.
            var bpm = Tmp("BPM", rt, font, "72 BPM", 32, TextLight, TextAlignmentOptions.Left);
            Stretch(bpm.rectTransform);
            bpm.rectTransform.offsetMin = new Vector2(48, 0);
            bpm.textWrappingMode = TextWrappingModes.NoWrap;
            bpm.overflowMode = TextOverflowModes.Overflow;
            return rt.gameObject;
        }

        // ------------------------------------------------------------ окна

        static GameObject BuildFullscreen(RectTransform hud, TMP_FontAsset font, out TextMeshProUGUI text)
        {
            var root = Stretch(UI("FullscreenText", hud));
            var dim = root.gameObject.AddComponent<Image>();
            // Затемнение ловит клики, чтобы клик по записке не уходил в кнопку телефона под ней.
            dim.color = new Color(0f, 0f, 0f, 0.82f);

            var paper = UI("Paper", root);
            Place(paper, new Vector2(0, 30), new Vector2(1100, 560));
            var pimg = paper.gameObject.AddComponent<Image>();
            pimg.color = Paper;
            pimg.raycastTarget = false;
            // Лёгкий наклон — бумажка, приклеенная к монитору, а не окно программы.
            paper.localRotation = Quaternion.Euler(0, 0, -1.5f);

            text = Tmp("Text", paper, font, "NOTE", 54, Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 60);
            text.textWrappingMode = TextWrappingModes.Normal;

            var hint = Tmp("Hint", root, font, "E — close", 28, TextDim, TextAlignmentOptions.Center);
            Place(hint.rectTransform, new Vector2(0, -330), new Vector2(800, 50));

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static GameObject BuildControlsHint(RectTransform hud, TMP_FontAsset font)
        {
            var root = Stretch(UI("ControlsHint", hud));
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var panel = UI("Panel", root);
            Place(panel, Vector2.zero, new Vector2(620, 440));
            var pimg = panel.gameObject.AddComponent<Image>();
            pimg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            pimg.type = Image.Type.Sliced;
            pimg.color = new Color(0.07f, 0.08f, 0.08f, 0.95f);
            pimg.raycastTarget = false;

            var title = Tmp("Title", panel, font, "CONTROLS", 40, TextLight, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0, 160), new Vector2(560, 60));
            title.characterSpacing = 8;

            var lines = Tmp("Lines", panel, font,
                "A / D   —   Move\nE   —   Interact\nTab   —   Phone\nEsc   —   Pause",
                34, TextLight, TextAlignmentOptions.Center);
            Place(lines.rectTransform, new Vector2(0, -5), new Vector2(560, 230));
            lines.lineSpacing = 20;

            var hint = Tmp("Hint", panel, font, "Press E to continue", 26, TextDim, TextAlignmentOptions.Center);
            Place(hint.rectTransform, new Vector2(0, -175), new Vector2(560, 40));

            root.gameObject.SetActive(false);
            return root.gameObject;
        }
    }
}
