using System;
using System.Collections;
using System.Collections.Generic;
using GC26.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Overworked
{
    /// <summary>
    /// Мини-игра 4: Tiny UFO на аркадном автомате Room_102. Грузит сцену gc26_spinv_Main additive
    /// поверх этажа, ждёт конца (победа — exit(1), QUIT — exit(0)) и выгружает её.
    /// На время игры этаж «гаснет»: выключаются камера, рендеры, коллайдеры, 2D-свет и HUD Overworked —
    /// иначе камера автомата (у начала координат) снимает коридор, а пули задевают его коллайдеры.
    /// Код игры — Assets/Microgames/gc26_spinv, разбор — docs/TINYUFO_PORT.md.
    /// </summary>
    public class TinyUfoMiniGame : MonoBehaviour, IMiniGame
    {
        [Tooltip("Имя сцены Tiny UFO (должна быть в Build Settings)")]
        [SerializeField] string sceneName = "gc26_spinv_Main";

        [Tooltip("Путь сцены — для запуска в редакторе, пока её нет в Build Settings")]
        [SerializeField] string scenePath = "Assets/Microgames/gc26_spinv/Scenes/gc26_spinv_Main.unity";

        // Что выключили на время игры — вернём ровно это, не трогая выключенное кем-то ещё.
        readonly List<Behaviour> hiddenBehaviours = new List<Behaviour>();
        readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        readonly List<Collider2D> hiddenColliders = new List<Collider2D>();
        readonly List<GameObject> hiddenObjects = new List<GameObject>();

        bool running;

        /// <inheritdoc/>
        public void Begin(Action<bool> onFinish)
        {
            if (running) return;
            // Корутина на MiniGameHost (_Systems): он живёт всю игру, а наш объект выключится вместе с этажом.
            var runner = MiniGameHost.Instance != null ? (MonoBehaviour)MiniGameHost.Instance : this;
            runner.StartCoroutine(Run(onFinish));
        }

        IEnumerator Run(Action<bool> onFinish)
        {
            running = true;
            var floor = SceneManager.GetActiveScene();

            var load = LoadScene();
            if (load == null)
            {
                Debug.LogError($"[MiniGames] Tiny UFO: сцена {sceneName} не найдена (Build Settings)");
                running = false;
                onFinish(false);
                yield break;
            }
            yield return load;

            var tiny = SceneManager.GetSceneByName(sceneName);
            var game = FindMicrogame(tiny);
            if (game == null)
            {
                Debug.LogError("[MiniGames] Tiny UFO: в сцене нет IMicrogame");
                yield return SceneManager.UnloadSceneAsync(tiny);
                running = false;
                onFinish(false);
                yield break;
            }

            HideFloor(floor, tiny);
            // Пули, взрывы и монеты Tiny UFO спавнит в активную сцену и потом чистит по своей сцене.
            SceneManager.SetActiveScene(tiny);

            int? result = null;
            game.Begin(score => result ??= score);
            while (result == null) yield return null;

            yield return SceneManager.UnloadSceneAsync(tiny);
            // Tiny UFO останавливает время и звук на паузе и в конце партии — возвращаем как было.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            if (floor.IsValid() && floor.isLoaded) SceneManager.SetActiveScene(floor);
            RestoreFloor();
            running = false;
            onFinish(result == 1);
        }

        AsyncOperation LoadScene()
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
#if UNITY_EDITOR
            // В редакторе сцену можно запустить и без Build Settings (пока P1 её туда не добавил).
            Debug.LogWarning($"[MiniGames] {sceneName} нет в Build Settings — грузим по пути (только редактор)");
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            return null;
#endif
        }

        static IMicrogame FindMicrogame(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var game = root.GetComponentInChildren<IMicrogame>(true);
                if (game != null) return game;
            }
            return null;
        }

        void HideFloor(Scene floor, Scene tiny)
        {
            hiddenBehaviours.Clear();
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            hiddenObjects.Clear();

            foreach (var root in floor.GetRootGameObjects())
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>()) Hide(r);
                foreach (var c in root.GetComponentsInChildren<Collider2D>()) Hide(c);
                foreach (var l in root.GetComponentsInChildren<Light2D>()) Hide(l);
                // Камера этажа: иначе две MainCamera и два AudioListener, Camera.main у Tiny UFO путается.
                foreach (var cam in root.GetComponentsInChildren<Camera>()) HideObject(cam.gameObject);
            }

            // HUD, подсказки и карточка задания Overworked (экранные канвасы в DontDestroyOnLoad).
            var ddol = gameObject.scene.name == "DontDestroyOnLoad" ? gameObject.scene : GetDontDestroyScene();
            if (ddol.IsValid())
                foreach (var root in ddol.GetRootGameObjects())
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                        if (canvas.isRootCanvas) Hide(canvas);

            // Клики по кнопкам автомата (PLAY/QUIT) обрабатывает родной EventSystem Tiny UFO:
            // с ним его кнопки и настроены. Наши EventSystem'ы (_Systems, меню) на время игры выключаем —
            // при двух живых Unity берёт текущим первый, наш, и клик до PLAY не доходил.
            foreach (var es in FindObjectsByType<EventSystem>())
                if (es.gameObject.scene != tiny) Hide(es);
            foreach (var root in tiny.GetRootGameObjects())
                foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                {
                    es.gameObject.SetActive(true);
                    EventSystem.current = es;
                }
        }

        // Сцена DontDestroyOnLoad недоступна по имени — достаём её через объект-зонд.
        static Scene GetDontDestroyScene()
        {
            if (MiniGameHost.Instance != null) return MiniGameHost.Instance.gameObject.scene;
            var probe = new GameObject("probe");
            DontDestroyOnLoad(probe);
            var scene = probe.scene;
            Destroy(probe);
            return scene;
        }

        void Hide(Behaviour b)
        {
            if (!b.enabled) return;
            b.enabled = false;
            hiddenBehaviours.Add(b);
        }

        void Hide(Renderer r)
        {
            if (!r.enabled) return;
            r.enabled = false;
            hiddenRenderers.Add(r);
        }

        void Hide(Collider2D c)
        {
            if (!c.enabled) return;
            c.enabled = false;
            hiddenColliders.Add(c);
        }

        void HideObject(GameObject go)
        {
            if (!go.activeSelf) return;
            go.SetActive(false);
            hiddenObjects.Add(go);
        }

        void RestoreFloor()
        {
            foreach (var go in hiddenObjects) if (go != null) go.SetActive(true);
            foreach (var b in hiddenBehaviours) if (b != null) b.enabled = true;
            foreach (var r in hiddenRenderers) if (r != null) r.enabled = true;
            foreach (var c in hiddenColliders) if (c != null) c.enabled = true;
            hiddenObjects.Clear();
            hiddenBehaviours.Clear();
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
        }
    }
}
