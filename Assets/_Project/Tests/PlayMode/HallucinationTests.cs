#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Overworked.Tests
{
    /// <summary>
    /// Галлюцинации в песочнице P4 «вся игра» (копии этажей, P4GameSandboxBuilder):
    /// рассудок падает, как только силуэт в кадре; спайк пульса не выше 140; финальный скример → WhiteRoom;
    /// переходы между этажами ведут по копиям.
    /// </summary>
    public class HallucinationTests
    {
        Keyboard keyboard;
        InputSettings.BackgroundBehavior oldBackground;
        InputSettings.EditorInputBehaviorInPlayMode oldEditor;

        [SetUp]
        public void SetUp()
        {
            // В batchmode нет окна Game с фокусом — без этого виртуальная клавиатура сбрасывается.
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        }

        void Hold(Key key, bool down)
        {
            InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(key) : new KeyboardState());
            InputSystem.Update();
        }

        static IEnumerator LoadCopy(string floor)
        {
            EditorSceneManager.LoadSceneInPlayMode($"Assets/_Project/Scenes/Sandbox/Sandbox_P4_{floor}.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForSeconds(1.5f);
            Assert.IsNotNull(PlayerController.Instance, $"в копии {floor} нет игрока");
            Assert.IsNotNull(HallucinationManager.Instance, "нет HallucinationManager в _Systems");
            PlayerController.Instance.SetInputLocked(false);
        }

        static HallucinationEvent Find(string prefix)
        {
            foreach (var h in Object.FindObjectsByType<HallucinationEvent>(FindObjectsInactive.Include))
                if (h.name.StartsWith(prefix)) return h;
            return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Silhouette_SanityDropsOnSight_SpikeOnApproach_Max140()
        {
            yield return LoadCopy("Floor_02");
            var h2 = Find("H2_");
            Assert.IsNotNull(h2, "в копии Floor_02 нет H2");
            var body = h2.GetComponentInChildren<SpriteRenderer>(true);

            HallucinationManager.Instance.Play(h2);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(body.enabled, "силуэт не появился");
            Assert.IsTrue(h2.SanityApplied, "силуэт в кадре, а рассудок не упал");
            Assert.IsFalse(h2.HasHit, "удар раньше, чем Дэнни подошёл");
            Assert.AreEqual(70f, SanityManager.Instance.Target, 0.01f);

            float maxBpm = 0f;
            Hold(Key.D, true);
            for (float t = 0f; t < 15f && !h2.HasHit; t += Time.deltaTime) yield return null;
            Hold(Key.D, false);
            Assert.IsTrue(h2.HasHit, "силуэт не среагировал на подход");
            for (float t = 0f; t < 5f; t += Time.deltaTime)
            {
                maxBpm = Mathf.Max(maxBpm, HeartbeatManager.Instance.Bpm);
                yield return null;
            }
            Assert.Greater(maxBpm, 100f, "пульс не подскочил");
            Assert.LessOrEqual(maxBpm, 140.01f, "спайк выше 140");
            Assert.IsFalse(body.enabled, "силуэт не исчез");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FinalScreamer_OnlyAfterTinyUfoWin_OnExit102_ThenWhiteRoom()
        {
            yield return LoadCopy("Floor_01");
            yield return CheckFinalScreamer();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator RealFloor01_FinalScreamer_AfterTinyUfoWin_ThenWhiteRoom()
        {
            P4SandboxRoute.Deactivate();
            SceneManager.LoadScene("Floor_01");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual("Floor_01", SceneManager.GetActiveScene().name);
            Assert.IsNotNull(PlayerController.Instance, "в Floor_01 нет игрока");
            PlayerController.Instance.SetInputLocked(false);
            yield return CheckFinalScreamer();
        }

        /// <summary>Без победы выход из 102 ничего не запускает; победа + выход → скример, рассудок 0, WhiteRoom.</summary>
        IEnumerator CheckFinalScreamer()
        {
            // Флаг победы TinyUfoWon живёт в GameManager между тестами — как «новая игра».
            GameManager.Instance.ResetState();
            var h5 = Find("H5_");
            Assert.IsNotNull(h5, "в копии Floor_01 нет H5");
            Room room = null;
            foreach (var r in Object.FindObjectsByType<Room>(FindObjectsInactive.Include))
                if (r.name == "Room_102") room = r;
            var arcade = Object.FindAnyObjectByType<ArcadeInteractable>();
            Assert.IsNotNull(room, "у Room_102 нет компонента Room");
            Assert.IsNotNull(arcade, "нет аркадного автомата");
            Assert.IsNotNull(Object.FindAnyObjectByType<FinalScreamerStarter>(), "нет FinalScreamerStarter");

            // Вышел без победы — ничего.
            room.MarkExited();
            yield return new WaitForSeconds(1.5f);
            Assert.IsFalse(h5.IsPlaying || h5.HasPlayed, "скример без победы в Tiny UFO");

            // Победа в Tiny UFO (onDone автомата), затем выход из 102.
            arcade.onDone.Invoke();
            room.MarkEntered();
            room.MarkExited();
            for (float t = 0f; t < 25f && !h5.HasHit; t += Time.deltaTime) yield return null;
            Assert.IsTrue(h5.HasHit, "скример не случился после выхода из 102");
            Assert.IsNotNull(GameObject.Find("ScreamerSquare"), "квадрат скримера не на экране");
            Assert.AreEqual(0f, SanityManager.Instance.Target, 0.01f);
            Assert.AreEqual(PlayerState.Locked, PlayerController.Instance.State, "управление не заблокировано");

            for (float t = 0f; t < 40f && SceneManager.GetActiveScene().name != "WhiteRoom"; t += Time.deltaTime) yield return null;
            Assert.AreEqual("WhiteRoom", SceneManager.GetActiveScene().name, "после скримера не перенесло в WhiteRoom");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator H3_FlickersSceneLights_ThenRestores()
        {
            yield return LoadCopy("Floor_02");
            var h3 = Find("H3_");
            Assert.IsNotNull(h3, "в копии Floor_02 нет H3");
            UnityEngine.Rendering.Universal.Light2D global = null;
            foreach (var l in Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsInactive.Exclude))
                if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) global = l;
            Assert.IsNotNull(global, "нет Global Light 2D");
            float baseline = global.intensity;

            bool done = false, changed = false;
            HallucinationManager.Instance.Play(h3, () => done = true);
            for (float t = 0f; t < 10f && !done; t += Time.deltaTime)
            {
                if (Mathf.Abs(global.intensity - baseline) > 0.001f) changed = true;
                yield return null;
            }
            Assert.IsTrue(done, "H3 не закончилась");
            Assert.IsTrue(changed, "свет сцены не мигал");
            Assert.AreEqual(baseline, global.intensity, 0.001f, "свет не вернулся к обычной яркости");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Stairs_LeadToSandboxCopies()
        {
            yield return LoadCopy("Floor_03");
            GameManager.Instance.LoadFloor(2);
            for (float t = 0f; t < 10f && SceneManager.GetActiveScene().name != "Sandbox_P4_Floor_02"; t += Time.deltaTime) yield return null;
            Assert.AreEqual("Sandbox_P4_Floor_02", SceneManager.GetActiveScene().name, "переход ушёл не в копию");
            yield return new WaitForSeconds(2f);
            Assert.IsNotNull(PlayerController.Instance, "в копии нет игрока после перехода");
            Assert.IsNotNull(Find("H2_"), "в копии Floor_02 нет галлюцинаций");
        }
    }
}
#endif
