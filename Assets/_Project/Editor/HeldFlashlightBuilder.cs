using System.IO;
using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Фонарик в руке Дэнни: импорт спрайта (PPU 1088 ≈ 0.41 юнита, пивот на рукоятке), мягкое свечение линзы
    /// и компонент HeldFlashlight на префабе Player с точками кисти по кадрам анимации ходьбы.
    /// Меню: Overworked → P1 → Setup Held Flashlight. Повторный запуск пересоздаёт объекты фонарика в префабе.
    /// </summary>
    public static class HeldFlashlightBuilder
    {
        const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string FlashlightPath = "Assets/_Project/Art/Props/Flashlight_Held.png";
        const string GlowPath = "Assets/_Project/Art/Effects/Lens_Glow.png";
        const string DannyDir = "Assets/_Project/Art/Characters/Danny/";
        // Картинки «только кисть» по кадрам (тот же размер кадра 300 × 430, PPU 136, пивот в центре).
        const string HandsDir = "Assets/_Project/Art/Characters/Danny/Hands/";
        const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        // Фонарик 448 × 112: рукоятку держит кисть (x ≈ 330), центр линзы — x ≈ 70.
        const float FlashlightPpu = 1088f;
        static readonly Vector2 GripPivot = new Vector2(330f / 448f, 0.5f);

        // Кисть ближней (правой) руки на кадрах Дэнни, пиксели кадра 300 × 430 от левого верхнего угла.
        // Найдены по цвету кожи (нижний край руки, выбор по непрерывности между кадрами); на кадрах 4_3–5_2,
        // где рука уходит назад в тени, — по листу вручную. Поправить можно в инспекторе HeldFlashlight.
        static readonly (string frame, Vector2 px)[] Hand =
        {
            ("Danny_Idle", new Vector2(144, 276)), ("Danny_Walk_0", new Vector2(144, 292)), ("Danny_Walk_0_1", new Vector2(143, 289)),
            ("Danny_Walk_0_2", new Vector2(143, 287)), ("Danny_Walk_0_3", new Vector2(144, 286)), ("Danny_Walk_0_4", new Vector2(144, 284)),
            ("Danny_Walk_1", new Vector2(144, 281)), ("Danny_Walk_1_1", new Vector2(140, 276)), ("Danny_Walk_1_2", new Vector2(134, 268)),
            ("Danny_Walk_1_3", new Vector2(232, 240)), ("Danny_Walk_1_4", new Vector2(231, 246)), ("Danny_Walk_2", new Vector2(229, 250)),
            ("Danny_Walk_2_1", new Vector2(230, 245)), ("Danny_Walk_2_2", new Vector2(231, 240)), ("Danny_Walk_2_3", new Vector2(132, 266)),
            ("Danny_Walk_2_4", new Vector2(137, 271)), ("Danny_Walk_3", new Vector2(141, 277)), ("Danny_Walk_3_1", new Vector2(141, 276)),
            ("Danny_Walk_3_2", new Vector2(144, 277)), ("Danny_Walk_4", new Vector2(146, 276)), ("Danny_Walk_4_1", new Vector2(144, 271)),
            ("Danny_Walk_4_2", new Vector2(141, 266)), ("Danny_Walk_4_3", new Vector2(100, 269)), ("Danny_Walk_4_4", new Vector2(96, 263)),
            ("Danny_Walk_5", new Vector2(90, 270)), ("Danny_Walk_5_1", new Vector2(90, 270)), ("Danny_Walk_5_2", new Vector2(94, 264)),
            ("Danny_Walk_5_3", new Vector2(136, 280)), ("Danny_Walk_5_4", new Vector2(140, 286)),
        };

        [MenuItem("Overworked/P1/Setup Held Flashlight")]
        public static void Run()
        {
            var flashSprite = ImportFlashlight();
            ImportHands();
            var glowSprite = EnsureGlow();
            if (flashSprite == null) { Debug.LogError($"[Setup] нет {FlashlightPath}"); return; }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var bodyT = root.transform.Find("Sprite");
                var body = bodyT != null ? bodyT.GetComponent<SpriteRenderer>() : null;
                if (body == null) { Debug.LogError("[Setup] в префабе Player нет Sprite со SpriteRenderer"); return; }
                foreach (var old in new[] { "HeldFlashlight", "LensGlow", "HandMask" })
                {
                    var t = root.transform.Find(old);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
                var oldOverlay = bodyT.Find("HandOverlay");
                if (oldOverlay != null) Object.DestroyImmediate(oldOverlay.gameObject);

                // Слои: Дэнни (body) → фонарик (+1) → кисть поверх фонарика (+2, картинка «только кисть») → свечение линзы (+3).
                // За телом фонарик пропадал, когда кисть на фоне штанов; поверх руки — выглядел приклеенным.
                var flash = new GameObject("HeldFlashlight").AddComponent<SpriteRenderer>();
                flash.transform.SetParent(root.transform, false);
                flash.sprite = flashSprite;
                flash.color = body.color;
                flash.sharedMaterial = body.sharedMaterial;
                flash.sortingLayerID = body.sortingLayerID;
                flash.sortingOrder = body.sortingOrder + 1;

                // Кисть поверх фонарика — дочерняя к спрайту Дэнни (тот же масштаб и позиция), картинка «только кисть»
                // текущего кадра (Art/Characters/Danny/Hands) ставит HeldFlashlight каждый кадр.
                var overlay = new GameObject("HandOverlay").AddComponent<SpriteRenderer>();
                overlay.transform.SetParent(bodyT, false);
                overlay.color = body.color;
                overlay.sharedMaterial = body.sharedMaterial;
                overlay.sortingLayerID = body.sortingLayerID;
                overlay.sortingOrder = body.sortingOrder + 2;

                // Свечение линзы: жёлтое, Unlit — светится и в тёмном коридоре; сплюснуто по X, как стекло в ракурсе.
                var glow = new GameObject("LensGlow").AddComponent<SpriteRenderer>();
                glow.transform.SetParent(root.transform, false);
                glow.transform.localScale = new Vector3(0.6f, 1f, 1f);
                glow.sprite = glowSprite;
                glow.color = new Color(1f, 0.9f, 0.55f, 0.9f);
                var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
                if (unlit != null) glow.sharedMaterial = unlit;
                glow.sortingLayerID = body.sortingLayerID;
                glow.sortingOrder = body.sortingOrder + 3;

                var held = root.GetComponent<HeldFlashlight>();
                if (held == null) held = root.AddComponent<HeldFlashlight>();
                var so = new SerializedObject(held);
                so.FindProperty("body").objectReferenceValue = body;
                so.FindProperty("flashlight").objectReferenceValue = flash;
                so.FindProperty("lensGlow").objectReferenceValue = glow;
                so.FindProperty("handOverlay").objectReferenceValue = overlay;
                so.FindProperty("lensOffset").vector2Value = new Vector2((70f - 330f) / FlashlightPpu, 0f);
                var list = so.FindProperty("handPoints");
                list.arraySize = 0;
                foreach (var (frame, px) in Hand)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(DannyDir + frame + ".png");
                    if (s == null) { Debug.LogWarning($"[Setup] нет кадра {frame}"); continue; }
                    list.arraySize++;
                    var e = list.GetArrayElementAtIndex(list.arraySize - 1);
                    e.FindPropertyRelative("Frame").objectReferenceValue = s;
                    e.FindPropertyRelative("Pixel").vector2Value = px;
                    e.FindPropertyRelative("Hand").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>(HandsDir + frame.Replace("Danny_", "Danny_Hand_") + ".png");
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                Debug.Log("[Setup] Player: фонарик в руке и свечение линзы");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Кисти — как кадры Дэнни: PPU 136, пивот в центре, без сжатия (ложатся ровно поверх кадра).</summary>
        static void ImportHands()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HandsDir.TrimEnd('/') }))
            {
                var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 136f;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                var st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
            }
        }

        static Sprite ImportFlashlight()
        {
            if (!(AssetImporter.GetAtPath(FlashlightPath) is TextureImporter ti)) return null;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = FlashlightPpu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = GripPivot;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(FlashlightPath);
        }

        /// <summary>Мягкое белое пятно 64×64 (цвет задаёт SpriteRenderer): PPU 400 ≈ 0.16 юнита.</summary>
        static Sprite EnsureGlow()
        {
            if (!File.Exists(GlowPath))
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
                File.WriteAllBytes(GlowPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(GlowPath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(GlowPath);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 400f;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
        }
    }
}
