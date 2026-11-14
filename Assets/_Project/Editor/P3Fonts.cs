using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: шрифты интерфейса для сборщиков P3. Hardpixel — субтитры (реплики, описания) и всплывающие окна
    /// (тост, записка, подсказка управления). Создаёт TMP-ассет из .otf один раз; запасной шрифт — PT Sans
    /// (символы, которых нет в Hardpixel). Готовый ассет не пересоздаётся.
    /// </summary>
    internal static class P3Fonts
    {
        const string MainFontPath = "Assets/_Project/Fonts/PTSans SDF.asset";
        const string HardpixelOtf = "Assets/_Project/Fonts/Hardpixel.otf";
        const string HardpixelAsset = "Assets/_Project/Fonts/Hardpixel SDF.asset";

        /// <summary>Шрифт диалогов и всплывающих окон. Нет файла шрифта — PT Sans.</summary>
        internal static TMP_FontAsset Dialogue()
        {
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            return Ensure(HardpixelOtf, HardpixelAsset, fallback, cyrillic: true) ?? fallback;
        }

        internal static TMP_FontAsset Ensure(string fontPath, string assetPath, TMP_FontAsset fallback, bool cyrillic)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogWarning($"[Setup] нет {fontPath} — остаётся PT Sans");
                return null;
            }
            var fa = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024);
            fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fa, assetPath);
            // Атлас и материал внутри ассета шрифта, иначе после перезапуска они теряются.
            fa.atlasTextures[0].name = fa.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            var chars = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) chars.Append(c);
            if (cyrillic)
            {
                for (char c = 'А'; c <= 'я'; c++) chars.Append(c);
                chars.Append("Ёё");
            }
            chars.Append("—–…«»’“”");
            fa.TryAddCharacters(chars.ToString());
            if (fallback != null) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            var so = new SerializedObject(fa);
            var clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null) clear.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }
    }
}
