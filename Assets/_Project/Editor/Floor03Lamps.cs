using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Лампы коридора Floor_03 получают настоящий свет и связываются с Галлюцинацией 1:
    /// для каждой пары «трубка Light_Tube_On + конус Light_Cone» в Environment/Corridor/Lights — объект Lamp_N
    /// с Light2D (под конусом) и LampLight (картинки повторяют яркость света). Затем в H1 этого этажа
    /// мерцанию (LightFlickerVisual «LampLights») отдаются именно эти лампы — «лампы резко мигают» (нарратив 1.3)
    /// мигают сами лампы, а не только вспышка на экране. Вспышка экрана H1 остаётся.
    /// Меню: Overworked → P2 → Floor_03 Lamps For Hallucination 1.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor03Lamps
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RequestFile = "Temp/p2_floor03_lamps.request";

        // Светильники на потолке, нарисованные на фоне коридора (Floor03_Corridor.png, 40 px/юнит, центр в 0):
        // X центров тёмных трубок. У первой (X ≈ -21.46) трубка и конус уже стоят — остальным ставим такие же.
        static readonly float[] FixtureX = { -16.14f, -10.8f, -0.64f, 4.69f, 13.55f, 19.41f };

        static Floor03Lamps()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Build();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        static void AddMissingFixtures(Transform lightsRoot)
        {
            SpriteRenderer tubeTemplate = null, coneTemplate = null;
            foreach (var sr in lightsRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (tubeTemplate == null && sr.name == "Light_Tube_On") tubeTemplate = sr;
                if (coneTemplate == null && sr.name == "Light_Cone") coneTemplate = sr;
            }
            if (tubeTemplate == null)
            {
                Debug.LogWarning("[Setup] Floor_03: нет образца Light_Tube_On — светильники не добавлены");
                return;
            }
            foreach (float x in FixtureX)
            {
                bool has = false;
                foreach (var sr in lightsRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.name.StartsWith("Light_Tube") && Mathf.Abs(sr.transform.position.x - x) < 1f) has = true;
                if (has) continue;
                Copy(tubeTemplate, lightsRoot, x);
                if (coneTemplate != null) Copy(coneTemplate, lightsRoot, x);
            }
        }

        static void Copy(SpriteRenderer template, Transform parent, float x)
        {
            var go = Object.Instantiate(template.gameObject, parent);
            go.name = template.name;
            var p = template.transform.position;
            go.transform.position = new Vector3(x, p.y, p.z);
            go.transform.localScale = template.transform.localScale;
        }

        [MenuItem("Overworked/P2/Floor_03 Lamps For Hallucination 1")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform lightsRoot = null;
            HallucinationEvent h1 = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Environment") lightsRoot = root.transform.Find("Corridor/Lights");
                foreach (var h in root.GetComponentsInChildren<HallucinationEvent>(true))
                    if (h.name.StartsWith("H1")) h1 = h;
            }
            if (lightsRoot == null)
            {
                Debug.LogError("[Setup] Floor_03: нет Environment/Corridor/Lights");
                return;
            }

            // Трубка и конус на каждый светильник с картинки — копии первой лампы (спрайт, слой, масштаб, высота).
            AddMissingFixtures(lightsRoot);

            // Конус без «лужи света» у пола: в Light_Cone.png у нижнего края яркая полоса — после подрезки
            // по полу она висела над полом. Light_Cone_Soft — та же картинка, полоса убрана, низ растворяется.
            var softCone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Effects/Light_Cone_Soft.png");
            if (softCone != null)
                foreach (var sr in lightsRoot.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.name.StartsWith("Light_Cone")) sr.sprite = softCone;

            // Старые Lamp_N — убрать (пересборка без дублей).
            for (int i = lightsRoot.childCount - 1; i >= 0; i--)
                if (lightsRoot.GetChild(i).name.StartsWith("Lamp_") || lightsRoot.GetChild(i).name == "BackgroundDimmer")
                    Object.DestroyImmediate(lightsRoot.GetChild(i).gameObject);

            var tubes = new List<SpriteRenderer>();
            var cones = new List<SpriteRenderer>();
            foreach (var sr in lightsRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name.StartsWith("Light_Tube")) tubes.Add(sr);
                else if (sr.name.StartsWith("Light_Cone")) cones.Add(sr);
            }

            var lamps = new List<Light2D>();
            var links = new List<LampLight>();
            int n = 0;
            foreach (var tube in tubes)
            {
                // Конус этой лампы — ближайший по X.
                SpriteRenderer cone = null;
                float best = float.MaxValue;
                foreach (var c in cones)
                {
                    float d = Mathf.Abs(c.transform.position.x - tube.transform.position.x);
                    if (d < best) { best = d; cone = c; }
                }
                if (cone != null && best > 2f) cone = null;

                var go = new GameObject($"Lamp_{++n}");
                go.transform.SetParent(lightsRoot, false);
                // Свет — посередине конуса (или чуть ниже трубки, если конуса нет).
                var pos = cone != null ? cone.bounds.center : tube.transform.position + Vector3.down * 2f;
                go.transform.position = new Vector3(tube.transform.position.x, pos.y, 0f);

                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(0.85f, 0.95f, 0.85f);
                light.intensity = 0.6f;
                light.pointLightInnerRadius = 0.5f;
                light.pointLightOuterRadius = cone != null ? Mathf.Max(2.5f, cone.bounds.size.y * 0.8f) : 3f;

                var link = go.AddComponent<LampLight>();
                var so = new SerializedObject(link);
                so.FindProperty("tube").objectReferenceValue = tube;
                so.FindProperty("cone").objectReferenceValue = cone;
                so.ApplyModifiedPropertiesWithoutUndo();
                lamps.Add(light);
                links.Add(link);
            }

            // Фон коридора темнеет вместе с лампами: погасли — 50 %, горят — обычная яркость.
            var bgRoot = lightsRoot.parent != null ? lightsRoot.parent.Find("Background") : null;
            if (bgRoot != null && links.Count > 0)
            {
                var dimmerGo = new GameObject("BackgroundDimmer");
                dimmerGo.transform.SetParent(lightsRoot, false);
                var dimmer = dimmerGo.AddComponent<BackgroundDimmer>();
                var dso = new SerializedObject(dimmer);
                var lampList = dso.FindProperty("lamps");
                lampList.arraySize = links.Count;
                for (int i = 0; i < links.Count; i++) lampList.GetArrayElementAtIndex(i).objectReferenceValue = links[i];
                var bg = bgRoot.GetComponentsInChildren<SpriteRenderer>(true);
                var bgList = dso.FindProperty("background");
                bgList.arraySize = bg.Length;
                for (int i = 0; i < bg.Length; i++) bgList.GetArrayElementAtIndex(i).objectReferenceValue = bg[i];
                dso.FindProperty("darkLevel").floatValue = 0.5f;
                // Темнеет всё на этаже: фон, двери, турникет, предметы, Дэнни (кроме самих ламп).
                dso.FindProperty("wholeScene").boolValue = true;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Setup] Floor_03: нет Corridor/Background — фон не затемняется");

            // H1: мерцание — по лампам этажа, а не по всему свету сцены.
            if (h1 != null)
            {
                // Белая вспышка на весь экран (LampFlash) убирается: «лампы резко мигают» — мигают сами лампы.
                // Её удар (звук + спайк) и рассудок забирает мерцание ламп. Префаб не меняем — распаковываем экземпляр.
                // Серая полупрозрачная пелена (Blur) — тоже экранный эффект, убирается вместе со вспышкой.
                foreach (var screenFx in new[] { "LampFlash", "Blur" })
                {
                    var fx = h1.transform.Find(screenFx);
                    if (fx == null) continue;
                    if (PrefabUtility.IsPartOfPrefabInstance(h1.gameObject))
                        PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(h1.gameObject),
                            PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Object.DestroyImmediate(fx.gameObject);
                }
                foreach (var flicker in h1.GetComponentsInChildren<LightFlickerVisual>(true))
                {
                    var so = new SerializedObject(flicker);
                    // Мерцание ламп — первое и главное: удар и рассудок на первом мигании, мигает заметно дольше.
                    so.FindProperty("WithPrevious").boolValue = false;
                    so.FindProperty("TriggersHit").boolValue = true;
                    so.FindProperty("SanityOnSeen").boolValue = true;
                    so.FindProperty("hitOnFirst").boolValue = true;
                    so.FindProperty("duration").floatValue = 2f;
                    var list = so.FindProperty("lights");
                    list.arraySize = lamps.Count;
                    for (int i = 0; i < lamps.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = lamps[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(flicker);
                }
            }
            else Debug.LogWarning("[Setup] Floor_03: нет H1 — лампы поставлены, но к галлюцинации не привязаны");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Floor_03: ламп со светом — {lamps.Count}, H1 мигает ими");
        }
    }
}
