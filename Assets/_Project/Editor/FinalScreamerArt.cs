using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Финальный скример H5: двойник Дэнни вместо тёмного прямоугольника и полноэкранный бросок вместо красного квадрата.
    /// — Двойник: кадры Art/Characters/Screamer/Walk/Doppelganger_Walk_0..7 (нарезка Doppelganger_Dark_Walk_strip,
    ///   ноги выровнены на 420 px, как у кадров Дэнни; PPU 136 — тот же рост). Идёт к Дэнни шагом (moveSpeed),
    ///   а не вспышками; SpriteWalkCycle крутит кадры, пока он движется.
    /// — Бросок: кадры Art/Characters/Screamer/Screamer_FullScreen/Doppelganger_ScreamerFS_00..09 на весь экран
    ///   (ScreamerVisual.frames), удар — на последнем.
    /// Меню: Overworked → P1 → Art → Dress Final Screamer (повторить, если H5 пересобран через Overworked → P4 → Hallucinations → Build Hallucinations).
    /// </summary>
    public static class FinalScreamerArt
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Hallucinations/H5_FinalScreamer.prefab";
        const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string WalkDir = "Assets/_Project/Art/Characters/Screamer/Walk/";
        const string FullDir = "Assets/_Project/Art/Characters/Screamer/Screamer_FullScreen/";
        const int WalkFrames = 8, FullFrames = 10;
        // Скорость шага двойника, юнитов в секунду (Дэнни ходит 1.5): медленнее — жутче.
        const float WalkSpeed = 1.1f;

        [MenuItem("Overworked/P1/Art/Dress Final Screamer")]
        public static void Run()
        {
            var walk = new Sprite[WalkFrames];
            for (int i = 0; i < WalkFrames; i++)
                if ((walk[i] = Import($"{WalkDir}Doppelganger_Walk_{i}.png", 136f, false)) == null) return;
            var full = new Sprite[FullFrames];
            for (int i = 0; i < FullFrames; i++)
                if ((full[i] = Import($"{FullDir}Doppelganger_ScreamerFS_{i:00}.png", 100f, true)) == null) return;

            // Смещение спрайта Дэнни от позиции игрока: двойник встаёт на тот же пол.
            float dannySpriteY = 0.2562f;
            var player = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var anim = player.GetComponentInChildren<Animator>();
                if (anim != null) dannySpriteY = anim.transform.localPosition.y;
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var s = root.GetComponentInChildren<SilhouetteBehaviour>(true);
                var body = s != null ? s.GetComponentInChildren<SpriteRenderer>(true) : null;
                if (body == null) { Debug.LogError("[Setup] в H5_FinalScreamer нет силуэта со SpriteRenderer"); return; }

                var sso = new SerializedObject(s);
                float offsetY = sso.FindProperty("offsetFromPlayer").vector2Value.y;
                sso.FindProperty("blinkSteps").intValue = 0;      // идёт шагом, а не вспышками
                sso.FindProperty("moveSpeed").floatValue = WalkSpeed;
                sso.ApplyModifiedPropertiesWithoutUndo();

                body.sprite = walk[0];
                body.color = Color.white;   // кадры уже тёмные — без чёрной заливки заглушки
                body.transform.localScale = Vector3.one;
                // Пивот кадра — центр, ноги на 420 из 430, как у Дэнни: та же высота спрайта над позицией игрока.
                body.transform.localPosition = new Vector3(0f, dannySpriteY - offsetY, 0f);
                if (!body.TryGetComponent(out SpriteWalkCycle cycle)) cycle = body.gameObject.AddComponent<SpriteWalkCycle>();
                var cso = new SerializedObject(cycle);
                var list = cso.FindProperty("frames");
                list.arraySize = walk.Length;
                for (int i = 0; i < walk.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = walk[i];
                // Сильное мерцание: часто пропадает, подолгу, чаще дважды подряд.
                cso.FindProperty("flicker").boolValue = true;
                cso.FindProperty("flickerGap").vector2Value = new Vector2(0.05f, 0.35f);
                cso.FindProperty("flickerOff").vector2Value = new Vector2(0.06f, 0.22f);
                cso.FindProperty("doubleFlicker").floatValue = 0.6f;
                cso.ApplyModifiedPropertiesWithoutUndo();

                var scream = root.GetComponentInChildren<ScreamerVisual>(true);
                if (scream != null)
                {
                    var vso = new SerializedObject(scream);
                    var fl = vso.FindProperty("frames");
                    fl.arraySize = full.Length;
                    for (int i = 0; i < full.Length; i++) fl.GetArrayElementAtIndex(i).objectReferenceValue = full[i];
                    vso.ApplyModifiedPropertiesWithoutUndo();
                }
                else Debug.LogWarning("[Setup] в H5_FinalScreamer нет ScreamerVisual");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Setup] H5_FinalScreamer: двойник ({walk.Length} кадров шага, {WalkSpeed} ю/с), бросок на весь экран ({full.Length} кадров)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite Import(string path, float ppu, bool fullScreen)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti))
            {
                Debug.LogError($"[Setup] нет {path}");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            // Полноэкранные кадры — 10 × 1920×1080: со сжатием, чтобы не держать в памяти ~80 МБ.
            ti.textureCompression = fullScreen ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = fullScreen ? 2048 : 1024;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
