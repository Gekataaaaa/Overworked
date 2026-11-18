using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Заготовки пяти сценарных галлюцинаций (паспорт, разделы 11 и 18; NARRATIVE_EN) на квадратах-заглушках:
    /// префабы в Prefabs/Hallucinations, реплики финала в Data/Dialogues и все пять в Sandbox_P4 (Shift+1…5).
    /// По этажам их расставляет P4GameSandboxBuilder (копии этажей в песочнице P4); в настоящие этажи
    /// их кладут владельцы сцен.
    /// Правила (по нарративу): рассудок падает в момент, когда Дэнни видит галлюцинацию; спайк пульса
    /// (до 140) — в резкий момент; финальный скример → падение, затемнение → WhiteRoom (FinalCollapse).
    /// Запуск: меню Overworked → P4 → Hallucinations → Build Hallucinations,
    /// или batchmode -executeMethod Overworked.EditorTools.HallucinationPrefabBuilder.Run.
    /// Готовые префабы и реплики не перезаписываются (там может быть арт).
    /// </summary>
    public static class HallucinationPrefabBuilder
    {
        const string Folder = "Assets/_Project/Prefabs/Hallucinations";
        const string DialogueFolder = "Assets/_Project/Data/Dialogues";
        const string Sandbox = "Assets/_Project/Scenes/Sandbox/Sandbox_P4.unity";

        public const string H1 = "H1_Turnstile", H2 = "H2_Silhouette", H3 = "H3_Room201", H4 = "H4_Room101", H5 = "H5_FinalScreamer";

        static readonly Color SilhouetteColor = new Color(0.02f, 0.02f, 0.03f);

        [MenuItem("Overworked/P4/Hallucinations/Build Hallucinations")]
        public static void Run()
        {
            var all = EnsurePrefabs();
            PlaceSandbox(all);
            Debug.Log("[Setup] P4: галлюцинации H1–H5 готовы (Prefabs/Hallucinations, Sandbox_P4)");
        }

        /// <summary>Создаёт недостающие префабы; возвращает H1…H5 по порядку.</summary>
        public static GameObject[] EnsurePrefabs()
        {
            var square = PlayerSandboxBuilder.EnsureSquareSprite();
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Hallucinations");
            var finalNo = EnsureDialogue("F1_FinalNo", "No…");
            var collapse = EnsureDialogue("F1_Collapse", "{name}…", "I can't anymore…");

            // H1: «Лампы резко мигают» — видно сразу (рассудок) и резко (спайк), затем картинка «плывёт».
            var h1 = Ensure(H1, go =>
            {
                Event(go, sanity: 90f, spike: 30f);
                Flash(go, "LampFlash", new Color(1f, 1f, 1f, 0.85f), 3, 0.07f, 0.12f, hit: true, seen: true);
                Flash(go, "Blur", new Color(0.6f, 0.6f, 0.6f, 0.3f), 6, 0.15f, 0.1f);
            });
            // H2: силуэт «появляется» (не резко) на границе луча — рассудок падает, как только он в кадре;
            // при подходе / свете фонарика — резкий звук, пульс резко учащается, силуэт исчезает.
            var h2 = Ensure(H2, go =>
            {
                Event(go, sanity: 70f, spike: 45f);
                Silhouette(go, square, new Vector2(0.9f, 1.9f), hit: true, offset: new Vector2(8.5f, 0.05f),
                    approach: 2.5f, flashlight: 6f, maxWait: 20f, speed: 0f, vanish: 0.35f);
                // Красное мерцание «Distortion» убрано из игры (P1, Remove Red Flicker).
            });
            // H3: дверь с грохотом захлопывается, свет бешено мигает — резко; сквозь помехи на миг «DD».
            var h3 = Ensure(H3, go =>
            {
                Event(go, sanity: 50f, spike: 50f);
                Flash(go, "LightFlicker", new Color(0f, 0f, 0f, 0.85f), 8, 0.1f, 0.12f, hit: true, seen: true);
                var noise = Visual<StaticNoiseVisual>(go, "Static", withPrevious: true, hit: false);
                Set(noise, "label", "DD");
            });
            // H4: лампы начинают мигать в такт пульсу (видно сразу — рассудок), в углу силуэт;
            // при приближении — резкий удар, пульс резко учащается.
            var h4 = Ensure(H4, go =>
            {
                Event(go, sanity: 25f, spike: 50f);
                var beat = Visual<ScreenFlashVisual>(go, "BeatFlicker", withPrevious: false, hit: false);
                beat.SanityOnSeen = true;
                Set(beat, "color", new Color(0f, 0f, 0f, 0.7f));
                Set(beat, "syncToHeartbeat", true);
                Set(beat, "onTime", 0.12f);
                Set(beat, "heartbeatDuration", 8f);
                Set(beat, "hitOnFirst", false);
                Silhouette(go, square, new Vector2(0.9f, 1.9f), hit: true, offset: new Vector2(5f, 0.05f),
                    approach: 2.5f, flashlight: 4f, maxWait: 20f, speed: 0f, vanish: 0.35f, withPrevious: true);
                // Красное мерцание «Distortion» убрано из игры (P1, Remove Red Flicker).
            });
            // H5: фигура идёт навстречу, Дэнни замирает — «No…», силуэт резко бросается (скример):
            // рассудок 25 → 0, испуг пульса, управление заблокировано; дальше FinalCollapse → WhiteRoom.
            var h5 = Ensure(H5, go =>
            {
                Event(go, sanity: 0f, spike: 0f, lockPlayer: true, keepLocked: true, screamer: true);
                var s = Silhouette(go, square, new Vector2(0.7f, 2.3f), hit: false, offset: new Vector2(9f, 0.25f),
                    approach: 3f, flashlight: 0f, maxWait: 12f, speed: 0f, vanish: 0f, seen: false);
                // Подходит за 3 «шага»: пропадает и резко появляется ближе.
                Set(s, "blinkSteps", 3);
                Set(s, "keepVisible", true);
                var say = Visual<DialogueVisual>(go, "SayNo", withPrevious: false, hit: false);
                Set(say, "lines", finalNo);
                var scream = Visual<ScreamerVisual>(go, "Screamer", withPrevious: false, hit: true);
                scream.SanityOnSeen = true;
                Set(go.AddComponent<FinalCollapse>(), "collapseLines", collapse);
            });
            // Мерцание настоящего света сцены (к конкретным лампам пока не привязано — весь свет, кроме фонарика).
            // Добавляется и в уже созданные префабы, рядом с полноэкранной вспышкой-заглушкой.
            EnsureLights(H1, "LampLights", after: "LampFlash", mode: LightFlickerVisual.Mode.Wild, duration: 0.6f, rate: 10f, bright: 1.8f);
            EnsureLights(H3, "RoomLights", after: "LightFlicker", mode: LightFlickerVisual.Mode.Wild, duration: 1.6f, rate: 16f, bright: 1f);
            EnsureLights(H4, "BeatLights", after: "BeatFlicker", mode: LightFlickerVisual.Mode.Heartbeat, duration: 8f, rate: 0f, bright: 1f);
            AssetDatabase.SaveAssets();
            return new[] { h1, h2, h3, h4, h5 };
        }

        // ------------------------------------------------------------ префабы

        /// <summary>
        /// Дочерний LightFlickerVisual «С предыдущим» сразу после визуала after — если в префабе его ещё нет.
        /// </summary>
        static void EnsureLights(string prefab, string name, string after, LightFlickerVisual.Mode mode, float duration, float rate, float bright)
        {
            string path = $"{Folder}/{prefab}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentInChildren<LightFlickerVisual>(true) != null) return;
                var anchor = root.transform.Find(after);
                var v = Visual<LightFlickerVisual>(root, name, withPrevious: true, hit: false);
                if (anchor != null) v.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);
                Set(v, "mode", (int)mode);
                Set(v, "duration", duration);
                if (rate > 0f) Set(v, "rate", rate);
                Set(v, "brightLevel", bright);
                Set(v, "hitOnFirst", false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static GameObject Ensure(string name, System.Action<GameObject> build)
        {
            string path = $"{Folder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var go = new GameObject(name);
            build(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static DialogueData EnsureDialogue(string name, params string[] lines)
        {
            string path = $"{DialogueFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (existing != null) return existing;
            var d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = Speaker.Danny, Text = lines[i] };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static void Event(GameObject go, float sanity, float spike, bool lockPlayer = false, bool keepLocked = false, bool screamer = false)
        {
            var e = go.AddComponent<HallucinationEvent>();
            e.SanityAfter = sanity;
            e.HeartbeatSpike = spike;
            e.Duration = 4f;
            e.LockPlayer = lockPlayer;
            e.KeepLocked = keepLocked;
            e.ScreamerHeartbeat = screamer;
        }

        static T Visual<T>(GameObject parent, string name, bool withPrevious, bool hit) where T : HallucinationVisual
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var v = go.AddComponent<T>();
            v.WithPrevious = withPrevious;
            v.TriggersHit = hit;
            return v;
        }

        static void Flash(GameObject parent, string name, Color color, int count, float on, float off,
            bool hit = false, bool seen = false, bool withPrevious = false)
        {
            var f = Visual<ScreenFlashVisual>(parent, name, withPrevious, hit);
            f.SanityOnSeen = seen;
            Set(f, "color", color);
            Set(f, "count", count);
            Set(f, "onTime", on);
            Set(f, "offTime", off);
        }

        static SilhouetteBehaviour Silhouette(GameObject parent, Sprite square, Vector2 size, bool hit, Vector2 offset,
            float approach, float flashlight, float maxWait, float speed, float vanish, bool withPrevious = false, bool seen = true)
        {
            var s = Visual<SilhouetteBehaviour>(parent, "Silhouette", withPrevious, hit);
            s.SanityOnSeen = seen;
            Set(s, "offsetFromPlayer", offset);
            Set(s, "approachDistance", approach);
            Set(s, "flashlightDistance", flashlight);
            Set(s, "maxWait", maxWait);
            Set(s, "moveSpeed", speed);
            Set(s, "vanishTime", vanish);

            var body = new GameObject("Body");
            body.transform.SetParent(s.transform, false);
            body.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = SilhouetteColor;
            // Чуть позади игрока (у него 10): силуэт не перекрывает Дэнни, когда тот подходит вплотную.
            sr.sortingOrder = 9;
            return s;
        }

        internal static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Setup] нет поля {field} у {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case float f: p.floatValue = f; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v: p.vector2Value = v; break;
                case Object o: p.objectReferenceValue = o; break;
                case null: p.objectReferenceValue = null; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ Sandbox_P4

        static void PlaceSandbox(GameObject[] all)
        {
            var scene = EditorSceneManager.OpenScene(Sandbox, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Hallucinations") Object.DestroyImmediate(go);
            var root = new GameObject("Hallucinations");
            foreach (var h in all)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(h, scene);
                go.transform.SetParent(root.transform, false);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
