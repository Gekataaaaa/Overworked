using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Floor_02: подбираемый фонарик (FlashlightPickup от P2, Floor02Flashlight) — вместо серого прямоугольника
    /// спрайт фонарика Flashlight_Held, лежащий на полу, того же размера и тона, что в руке Дэнни.
    /// Подбор, полёт над Дэнни, реплика и подсказка [F] — без изменений.
    /// Меню: Overworked → P1 → Art → Dress Floor_02 Flashlight Pickup (после Overworked → P2 → Floor_02 → Floor_02 Flashlight Pickup).
    /// </summary>
    public static class Floor02FlashlightArt
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_02.unity";
        const string SpritePath = "Assets/_Project/Art/Props/Flashlight_Held.png";
        const string GlowPath = "Assets/_Project/Art/Effects/Lens_Glow.png";
        // Тот же приглушённый тон, что у спрайта Дэнни и фонарика в руке.
        static readonly Color Tint = new Color(0.62f, 0.64f, 0.6f, 1f);

        [MenuItem("Overworked/P1/Art/Dress Floor_02 Flashlight Pickup")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError("[Setup] нет Flashlight_Held — сначала Overworked → P1 → Player → Setup Held Flashlight");
                return;
            }

            Transform pickup = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "FlashlightPickup") pickup = t;
            if (pickup == null)
            {
                Debug.LogError("[Setup] Floor_02: нет FlashlightPickup — сначала Overworked → P2 → Floor_02 → Floor_02 Flashlight Pickup");
                return;
            }

            // Низ серого прямоугольника — это пол; фонарик кладём на то же место.
            float bottom = -0.11f;
            var placeholder = pickup.Find("Sprite");
            if (placeholder != null)
            {
                bottom = placeholder.localPosition.y - placeholder.localScale.y / 2f;
                placeholder.gameObject.SetActive(false);
            }
            var old = pickup.Find("Art");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var art = new GameObject("Art").AddComponent<SpriteRenderer>();
            art.transform.SetParent(pickup, false);
            art.sprite = sprite;
            art.color = Tint;
            art.sortingOrder = -6;
            // Пивот спрайта — на рукоятке: сдвигаем, чтобы фонарик лежал по центру зоны подбора, низом на полу.
            var b = sprite.bounds;
            art.transform.localPosition = new Vector3(-b.center.x, bottom + b.extents.y - b.center.y, 0f);

            // Подсветка P2 (InteractionHighlight) обводит спрайт его копией, крупнее на 0.07 юнита с каждой стороны.
            // Фонарик в высоту ~0.1 юнита — копия выходила в 2.4 раза выше (и смещённой от пивота на рукоятке):
            // «фонарик в фонарике». Поэтому подсветка (через HighlightTarget) — на невидимое мягкое пятно
            // вокруг фонарика: вместо раздутой копии под ним мягкий жёлтый ореол.
            var oldProxy = pickup.Find("HighlightProxy");
            if (oldProxy != null) Object.DestroyImmediate(oldProxy.gameObject);
            var glow = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
            if (glow != null)
            {
                var proxy = new GameObject("HighlightProxy").AddComponent<SpriteRenderer>();
                proxy.transform.SetParent(pickup, false);
                proxy.transform.localPosition = new Vector3(0f, bottom + b.extents.y, 0f);
                var g = glow.bounds.size;
                proxy.transform.localScale = new Vector3(b.size.x * 1.1f / g.x, b.size.y * 2.2f / g.y, 1f);
                proxy.sprite = glow;
                proxy.color = new Color(1f, 1f, 1f, 0f);   // само пятно не видно — видна только жёлтая копия-ореол
                proxy.sortingOrder = -7;
                if (!pickup.TryGetComponent(out HighlightTarget ht)) ht = pickup.gameObject.AddComponent<HighlightTarget>();
                ht.target = proxy.gameObject;
                EditorUtility.SetDirty(ht);
            }
            else Debug.LogWarning("[Setup] нет Lens_Glow — сначала Overworked → P1 → Player → Setup Held Flashlight");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Floor_02: подбираемый фонарик — спрайт Flashlight_Held");
        }
    }
}
