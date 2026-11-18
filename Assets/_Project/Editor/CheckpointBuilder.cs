using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Платформа-чекпоинт (CheckpointPlatform) по центру коридора: тонкая плита шириной 1.5 Дэнни, от неё вверх
    /// столб света и партиклы, оба гаснут к высоте Дэнни. Коридор — выделенный в сцене (с Walls/WallLeft и
    /// Walls/WallRight) или Corridor_B. Повторный запуск пересобирает. После — сохранить сцену. Отмена — Ctrl+Z.
    /// </summary>
    public static class CheckpointBuilder
    {
        const string RootName = "Checkpoint";
        const string BeamPath = "Assets/_Project/Art/Props/CheckpointBeam.png";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        const string ParticlesPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
        const float WidthInPlayers = 1.5f;
        const float PlatformHeight = 0.08f;
        const float RiseSpeed = 0.8f;   // скорость партиклов вверх, юнит/с

        [MenuItem("Overworked/P1/Objects/Add Checkpoint To Corridor")]
        public static void Build()
        {
            var corridor = Selection.activeGameObject;
            if (corridor == null || corridor.transform.Find("Walls/WallLeft") == null) corridor = GameObject.Find("Corridor_B");
            var left = corridor != null ? corridor.transform.Find("Walls/WallLeft") : null;
            var right = corridor != null ? corridor.transform.Find("Walls/WallRight") : null;
            if (left == null || right == null)
            {
                Debug.LogWarning("[Checkpoint] выделите коридор в сцене (нужны Walls/WallLeft и Walls/WallRight) или откройте сцену с Corridor_B");
                return;
            }
            Undo.SetCurrentGroupName("Add Checkpoint");
            int undoGroup = Undo.GetCurrentGroup();
            try { BuildIn(corridor); }
            catch (System.Exception e)
            {
                // Упало посередине — убираем всё, что успели создать, чтобы в сцене не осталось половины.
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError($"[Checkpoint] не собрался, изменения отменены: {e}");
            }
        }

        static void BuildIn(GameObject corridor)
        {
            var root = corridor.transform;
            var left = root.Find("Walls/WallLeft");
            var right = root.Find("Walls/WallRight");
            var old = root.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            // Размер Дэнни — по хитбоксу префаба Player; низ ног — от EntryPoint коридора (там стоит его корень).
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var hitbox = playerPrefab != null ? playerPrefab.GetComponent<BoxCollider2D>() : null;
            Vector2 size = hitbox != null ? hitbox.size : new Vector2(0.8f, 2.9f);
            Vector2 offset = hitbox != null ? hitbox.offset : new Vector2(0f, 0.2f);
            var entry = root.Find("EntryPoint");
            float feetY = entry != null ? entry.localPosition.y + offset.y - size.y / 2f : -4.55f;
            float width = size.x * WidthInPlayers;
            float height = size.y;

            var go = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(go, "Add Checkpoint");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3((left.localPosition.x + right.localPosition.x) / 2f, feetY, 0f);

            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);

            // Плита.
            var plate = new GameObject("Platform");
            plate.transform.SetParent(go.transform, false);
            plate.transform.localPosition = new Vector3(0f, PlatformHeight / 2f, 0f);
            plate.transform.localScale = new Vector3(width, PlatformHeight, 1f);
            var plateSr = plate.AddComponent<SpriteRenderer>();
            plateSr.sprite = SB.EnsureSquareSprite();
            plateSr.sharedMaterial = unlit;
            plateSr.sortingOrder = 14;   // поверх ног Дэнни (10–13)

            // Столб света: градиент снизу вверх до прозрачного, ширина плиты, высота Дэнни.
            var beamSprite = EnsureBeamSprite();
            var beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            beam.transform.localPosition = new Vector3(0f, PlatformHeight, 0f);
            beam.transform.localScale = new Vector3(width / beamSprite.bounds.size.x, height / beamSprite.bounds.size.y, 1f);
            var beamSr = beam.AddComponent<SpriteRenderer>();
            beamSr.sprite = beamSprite;
            beamSr.sharedMaterial = unlit;
            beamSr.color = new Color(1f, 1f, 1f, 0.45f);
            beamSr.sortingOrder = 8;     // за Дэнни

            // Свет 2D: точечный, конусом вверх от плиты, гаснет к росту Дэнни. Отдельный объект без масштаба
            // (масштаб столба растянул бы свет).
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, PlatformHeight, 0f);
            var light = glowGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = height;
            light.pointLightInnerAngle = 20f;
            light.pointLightOuterAngle = 50f;
            light.intensity = 0.8f;

            var particles = BuildParticles(go.transform, width, height);

            // Триггер — плита и чуть выше, чтобы ноги Дэнни точно касались.
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, 1f);
            col.offset = new Vector2(0f, 0.5f);

            var cp = go.AddComponent<CheckpointPlatform>();
            var so = new SerializedObject(cp);
            so.FindProperty("platform").objectReferenceValue = plateSr;
            so.FindProperty("beam").objectReferenceValue = beamSr;
            so.FindProperty("glow").objectReferenceValue = light;
            so.FindProperty("particles").objectReferenceValue = particles;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(corridor.scene);
            Selection.activeGameObject = go;
            Debug.Log($"[Checkpoint] {corridor.name}: чекпоинт по центру — сохраните сцену");
        }

        // Мелкие искры: рождаются по ширине плиты, поднимаются и исчезают к высоте Дэнни.
        static ParticleSystem BuildParticles(Transform parent, float width, float height)
        {
            var go = new GameObject("Particles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, PlatformHeight, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(height / RiseSpeed * 0.7f, height / RiseSpeed);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, 0.02f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(RiseSpeed * 0.85f, RiseSpeed);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticlesPath);
            r.sortingOrder = 9;
            return ps;
        }

        // Вертикальный градиент 8×64: снизу непрозрачный, к верху прозрачный (квадратично — гаснет мягко). Pivot снизу.
        static Sprite EnsureBeamSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(BeamPath);
            if (existing != null) return existing;
            const int w = 8, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float a = 1f - y / (float)(h - 1);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(BeamPath));
            File.WriteAllBytes(BeamPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(BeamPath);

            var imp = (TextureImporter)AssetImporter.GetAtPath(BeamPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 64f;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(BeamPath);
        }
    }
}
